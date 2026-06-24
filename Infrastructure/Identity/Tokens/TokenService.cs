using Application;
using Application.Exceptions;
using Application.Features.Tokens.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Constants;
using Infrastructure.Contexts;
using Infrastructure.Identity.Models;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Identity.Tokens
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
        private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantContextAccessor;
        private readonly JwtSettings _jwtSettings;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IMultiTenantContextAccessor<AppTenantInfo> tenantContextAccessor,
            IOptions<JwtSettings> jwtSettings,
            IMultiTenantStore<AppTenantInfo> tenantStore,
            ApplicationDbContext dbContext,
            ILogger<TokenService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantContextAccessor = tenantContextAccessor;
            _jwtSettings = jwtSettings.Value;
            _tenantStore = tenantStore;
            _dbContext = dbContext;
            _logger = logger;
        }


        // Login
        public async Task<TokenResponse> LoginAsync(TokenRequest request)
        {
            var userInDb = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumberOrEmail || u.Email == request.PhoneNumberOrEmail);

            if (userInDb == null)
                throw new UnauthorizedException(["Invalid username or password"]);

            if (await _userManager.IsLockedOutAsync(userInDb))
            {
                _logger.LogWarning("Security Alert: Locked out user {UserEmail} attempted to login from IP.", userInDb.Email);
                var lockoutEnd = userInDb.LockoutEnd;
                if (lockoutEnd.HasValue)
                {
                    var timeRemaining = lockoutEnd.Value.Subtract(DateTimeOffset.UtcNow);
                    int minutesRemaining = (int)Math.Ceiling(timeRemaining.TotalMinutes);

                    throw new UnauthorizedException([$"Account is temporarily locked due to multiple failed login attempts. Please try again after {minutesRemaining} minutes."]);
                }

                throw new UnauthorizedException(["Account is temporarily locked. Please try again later."]);
            }

            if (!await _userManager.CheckPasswordAsync(userInDb, request.Password))
            {
                await _userManager.AccessFailedAsync(userInDb);
                _logger.LogWarning("Failed login attempt for user {UserEmail}. Failed attempts count updated.", userInDb.Email);
                throw new UnauthorizedException(["Invalid username or password"]);
            }

            await _userManager.ResetAccessFailedCountAsync(userInDb);

            if (!userInDb.IsActive)
                throw new UnauthorizedException(["Account is deactivated — contact administrator"]);

            if (!userInDb.EmailConfirmed)
                throw new UnauthorizedException(["Email Not Verified"]);

            // Resolve the user's active workspaces — the source of truth for which
            // tenant the session is scoped to.
            var memberships = await _dbContext.WorkspaceMembers
                .Where(m => m.UserId == userInDb.Id && m.Status == WorkspaceMemberStatus.Active)
                .ToListAsync();

            // 0 memberships (students/parents) or 1 (a normal teacher/assistant) →
            // behave exactly as before: issue a full token straight away. For the
            // single-workspace case the tenant is that one membership.
            if (memberships.Count <= 1)
            {
                var singleTenantId = memberships.Count == 1 ? memberships[0].TenantId : null;
                return await GenerateTokenAndUpdateUserAsync(userInDb, singleTenantId);
            }

            // ≥2 workspaces → the user must choose one before we issue a full session.
            return await BuildWorkspaceSelectionResponseAsync(userInDb, memberships);
        }

        // Refresh Token
        public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var userPrincipal = GetClaimsPrincipalFromExpiredToken(request.CurrentJwtToken);
            var userId = userPrincipal.GetUserId();
            var jti = userPrincipal.FindFirstValue(JwtRegisteredClaimNames.Jti);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jti))
                throw new UnauthorizedException(["Invalid token claims."]);

            var userInDb = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedException(["Authentication failed"]);

            string hashedIncomingToken = HashRefreshToken(request.CurrentRefreshToken);

            var storedToken = await _dbContext.UserRefreshTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == hashedIncomingToken);


            if (storedToken == null)
            {
                await RevokeAllUserTokensAsync(userId); 
                throw new UnauthorizedException(["Security alert: Invalid token. For your security, you have been logged out from all devices."]);
            }

            if (storedToken.IsRevoked)
            {
                await RevokeAllUserTokensAsync(userId); 
                throw new UnauthorizedException(["Security alert: Suspicious activity detected (Token Reuse). You have been logged out from all devices."]);
            }

            if (storedToken.ExpiresOn < DateTime.UtcNow || storedToken.JwtId != jti)
            {
                throw new UnauthorizedException(["Invalid or expired refresh token. Please login again."]);
            }

            storedToken.IsRevoked = true;
            _dbContext.UserRefreshTokens.Update(storedToken);
            await _dbContext.SaveChangesAsync();

            // Keep the user in the SAME workspace across a refresh — carry the tenant
            // from the (now-expired) access token instead of re-resolving it.
            var currentTenantId = userPrincipal.FindFirstValue(ClaimConstants.Tenant);
            return await GenerateTokenAndUpdateUserAsync(userInDb, currentTenantId);
        }

        // ══════════════════════════════════════════════════
        // Generate Token + Update User
        // ══════════════════════════════════════════════════
        private async Task<TokenResponse> GenerateTokenAndUpdateUserAsync(ApplicationUser user, string? selectedTenantId)
        {
            string jti = Guid.NewGuid().ToString();
            var jwt = await GenerateTokenAsync(user, jti, selectedTenantId);
            string plainRefreshToken = GenerateRefreshToken();

            var expiredTokens = await _dbContext.UserRefreshTokens
                .Where(t => t.UserId == user.Id &&
                            (t.IsRevoked || t.ExpiresOn < DateTime.UtcNow))
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _dbContext.UserRefreshTokens.RemoveRange(expiredTokens);
            }

            var refreshTokenEntity = new UserRefreshToken
            {
                UserId = user.Id,
                TokenHash = HashRefreshToken(plainRefreshToken),
                JwtId = jti,
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryInDays),
                IsRevoked = false
            };

            await _dbContext.UserRefreshTokens.AddAsync(refreshTokenEntity);
            await _dbContext.SaveChangesAsync();

            return new TokenResponse
            {
                JwtToken = jwt,
                RefreshToken = plainRefreshToken,
                RefreshTokenExpiryDate = refreshTokenEntity.ExpiresOn
            };
        }

        // ══════════════════════════════════════════════════
        // Workspace selection / switching
        // ══════════════════════════════════════════════════
        public async Task<TokenResponse> SelectWorkspaceAsync(string userId, string tenantId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedException(["Authentication failed"]);

            if (string.IsNullOrWhiteSpace(tenantId))
                throw new UnauthorizedException(["A workspace must be specified."]);

            // The user can only sign into a workspace they're an active member of.
            var membership = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.UserId == userId
                                       && m.TenantId == tenantId
                                       && m.Status == WorkspaceMemberStatus.Active);

            if (membership == null)
                throw new ForbiddenException(["You don't have access to this workspace."]);

            return await GenerateTokenAndUpdateUserAsync(user, tenantId);
        }

        // Builds the "pick a workspace" response: a short-lived account token (no tenant,
        // no permissions — only good for /select-workspace) plus the list of choices.
        private async Task<TokenResponse> BuildWorkspaceSelectionResponseAsync(
            ApplicationUser user, List<WorkspaceMember> memberships)
        {
            return new TokenResponse
            {
                JwtToken = GenerateAccountToken(user),
                RefreshToken = string.Empty,
                RequiresWorkspaceSelection = true,
                Workspaces = await MapWorkspaceOptionsAsync(memberships)
            };
        }

        public async Task<List<WorkspaceOption>> GetUserWorkspacesAsync(string userId)
        {
            var memberships = await _dbContext.WorkspaceMembers
                .Where(m => m.UserId == userId && m.Status == WorkspaceMemberStatus.Active)
                .ToListAsync();

            return await MapWorkspaceOptionsAsync(memberships);
        }

        private async Task<List<WorkspaceOption>> MapWorkspaceOptionsAsync(List<WorkspaceMember> memberships)
        {
            var workspaces = new List<WorkspaceOption>();
            foreach (var m in memberships)
            {
                var tenantInfo = await _tenantStore.TryGetAsync(m.TenantId);
                workspaces.Add(new WorkspaceOption
                {
                    TenantId = m.TenantId,
                    Name = tenantInfo?.Name ?? m.TenantId,
                    Type = (tenantInfo?.Type ?? TenantType.Individual).ToString(),
                    Role = m.Role.ToString()
                });
            }
            return workspaces;
        }

        // A minimal, short-lived token that authenticates the user but carries no tenant
        // and no permissions — its only purpose is to authorise /select-workspace.
        private string GenerateAccountToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("pre_auth", "true"),
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: GenerateSigningCredentials());

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ══════════════════════════════════════════════════
        // Generate JWT
        // ══════════════════════════════════════════════════
        private async Task<string> GenerateTokenAsync(ApplicationUser user, string jti, string? selectedTenantId)
        {
            return GenerateEncryptedToken(
                GenerateSigningCredentials(),
                await GetUserClaimsAsync(user, jti, selectedTenantId));
        }

        private string GenerateEncryptedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
        {
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,     
                audience: _jwtSettings.Audience, 
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpiryInMinutes),
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private SigningCredentials GenerateSigningCredentials()
        {
            var secret = Encoding.UTF8.GetBytes(_jwtSettings.Secret);
            return new SigningCredentials(
                new SymmetricSecurityKey(secret),
                SecurityAlgorithms.HmacSha256);
        }

        // ══════════════════════════════════════════════════
        // User Claims + Permissions
        // ══════════════════════════════════════════════════
        private async Task<IEnumerable<Claim>> GetUserClaimsAsync(ApplicationUser user, string jti, string? selectedTenantId)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, jti), 
                new(ClaimTypes.GivenName,   user.FirstName),
                new(ClaimTypes.Surname,     user.LastName),
                new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
                new("SecurityStamp", user.SecurityStamp ?? string.Empty)
            };

            bool isSubscriptionExpired = false;
            var centerPermissions = CenterPermissions.None;
            // The tenant is the workspace the user selected at login — NOT a standalone
            // user claim (a user can now belong to several workspaces).
            var tenantIdClaim = selectedTenantId;

            if (!string.IsNullOrEmpty(tenantIdClaim))
            {
                claims.Add(new Claim(ClaimConstants.Tenant, tenantIdClaim));

                // Record the user's role in this workspace so per-request scoping
                // (e.g. a center member teacher only seeing their own groups) can rely
                // on the token instead of hitting the DB on every query.
                var membership = await _dbContext.WorkspaceMembers
                    .FirstOrDefaultAsync(m => m.UserId == user.Id
                                           && m.TenantId == tenantIdClaim
                                           && m.Status == WorkspaceMemberStatus.Active);
                if (membership != null)
                {
                    claims.Add(new Claim(ClaimConstants.WorkspaceRole, membership.Role.ToString()));
                    // Operational capabilities this member was granted inside the workspace.
                    centerPermissions = membership.Permissions;
                }

                var tenantInfo = await _tenantStore.TryGetAsync(tenantIdClaim);
                if (tenantInfo != null)
                {
                    // A center owner may log in even while the center is inactive so they can
                    // SEE the "not subscribed yet / expired" state and renew. Everyone else is
                    // blocked when their workspace is deactivated.
                    var isCenterOwner = membership?.Role == WorkspaceRole.Owner
                                        && tenantInfo.Type == TenantType.Center;

                    if (!tenantInfo.IsActive && !isCenterOwner)
                        throw new UnauthorizedException(["Your workspace is deactivated. Please contact platform support."]);

                    if (!tenantInfo.IsActive || tenantInfo.ValidUpTo < DateTime.UtcNow)
                    {
                        isSubscriptionExpired = true;
                    }

                    claims.Add(new Claim("Tenant_Type", tenantInfo.Type.ToString()));
                    claims.Add(new Claim("Tenant_IsActive", tenantInfo.IsActive.ToString()));
                    claims.Add(new Claim("Tenant_ValidUpTo", tenantInfo.ValidUpTo.ToString("O")));
                    claims.Add(new Claim("Tenant_IsExpired", isSubscriptionExpired.ToString())); 
                }
            }

            var roleClaims = new List<Claim>();
            var permissionClaims = new List<Claim>();

            foreach (var userRole in userRoles)
            {
                roleClaims.Add(new Claim(ClaimTypes.Role, userRole));
                var currentRole = await _roleManager.FindByNameAsync(userRole);
                if (currentRole is null) continue;

                var allPermissionsForRole = await _roleManager.GetClaimsAsync(currentRole);

                foreach (var permission in allPermissionsForRole)
                {
                    if (isSubscriptionExpired)
                    {
                        if (permission.Value.EndsWith($".{AppAction.Read}") ||
                            permission.Value.EndsWith($".{AppAction.RefreshToken}"))
                        {
                            permissionClaims.Add(permission);
                        }
                    }
                    else
                    {
                        permissionClaims.Add(permission);
                    }
                }
            }
            // Center-membership capability flags widen what this member can do in the
            // selected workspace, on top of their Identity-role permissions. When the
            // subscription has lapsed, only read access survives (same rule as above).
            foreach (var permissionName in CenterPermissionMap.ToPermissionNames(centerPermissions))
            {
                if (isSubscriptionExpired && !permissionName.EndsWith($".{AppAction.Read}"))
                    continue;
                permissionClaims.Add(new Claim(ClaimConstants.Permission, permissionName));
            }

            // Exclude any standalone tenant claim from the DB — the authoritative tenant
            // is the selected workspace, already added above.
            var allClaims = claims
                .Union(roleClaims)
                .Union(userClaims.Where(c => c.Type != ClaimConstants.Tenant))
                .Union(permissionClaims);

            return allClaims
                   .Select(c => new { c.Type, c.Value })
                   .Distinct()
                   .Select(c => new Claim(c.Type, c.Value));
        }

        // ══════════════════════════════════════════════════
        // Get Claims Principal from Expired Token
        // ══════════════════════════════════════════════════
        private ClaimsPrincipal GetClaimsPrincipalFromExpiredToken(
            string expiringToken)
        {
            var secret = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

            var tokenValidationParams = new TokenValidationParameters
            {
                // We ONLY validate the signature here — this method extracts the
                // principal from an ALREADY-EXPIRED access token during refresh.
                // Authenticity is fully established by: (1) this signature check,
                // (2) the DB refresh-token lookup, and (3) the jti match in
                // RefreshTokenAsync. Issuer/Audience/Lifetime are intentionally
                // NOT validated (the canonical GetPrincipalFromExpiredToken pattern).
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secret),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role,
                ValidateLifetime = false,
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(
                    expiringToken,
                    tokenValidationParams,
                    out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken
                    || !jwtSecurityToken.Header.Alg.Equals(
                        SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase))
                    throw new UnauthorizedException(
                        ["Invalid token provided — failed to generate new token"]);

                return principal;
            }
            catch (UnauthorizedException)
            {
                throw; // already the right type → surfaces as 401
            }
            catch (Exception)
            {
                // A malformed token or bad signature is an AUTH failure → 401 (so
                // the client re-authenticates), NOT an unexpected server error.
                throw new UnauthorizedException(["Invalid token. Please log in again."]);
            }
        }

        // ══════════════════════════════════════════════════
        // Generate Refresh Token
        // ══════════════════════════════════════════════════
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private static string HashRefreshToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public async Task LogoutAsync(string userId, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new UnauthorizedException(["Refresh token is required for logout."]);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException(["User not found."]);

            string hashedToken = HashRefreshToken(refreshToken);

            var storedToken = await _dbContext.UserRefreshTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == hashedToken);

            if (storedToken == null)
            {
                _logger.LogWarning("Logout: token not found for user {UserId} — possible reuse or tampering.", userId);
                throw new UnauthorizedException(["Invalid refresh token provided."]);
            }

            if (storedToken.IsRevoked)
            {
                _logger.LogInformation("Logout: token already revoked for user {UserId}.", userId);
                return;
            }

            storedToken.IsRevoked = true;
            await _dbContext.SaveChangesAsync();
        }

        private async Task RevokeAllUserTokensAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                await _userManager.UpdateAsync(user);
            }

            var activeTokens = await _dbContext.UserRefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}