using Application.Exceptions;
using Application.Features.Identity.Commands;
using Application.Features.Identity.DTOs;
using Application.Features.Tenancy;
using Application.Features.Tenancy.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Constants;
using Infrastructure.Contexts;
using Infrastructure.Identity.Models;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService,
            ApplicationDbContext dbContext,
            IEmailService emailService,
            ICurrentUserService currentUserService,
            IMultiTenantStore<AppTenantInfo> tenantStore)
        {
            _userManager = userManager;
            _tenantService = tenantService;
            _dbContext = dbContext;
            _emailService = emailService;
            _currentUserService = currentUserService;
            _tenantStore = tenantStore;
        }

        // ══════════════════════════════════════════════════
        // Register Student (Global Level)
        // ══════════════════════════════════════════════════
        public async Task<string> RegisterStudentAsync(RegisterStudentRequest request, CancellationToken ct = default)
        {
            var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null && existingEmail.EmailConfirmed)
                throw new ConflictException(["This email is already registered and verified."]);

            // ── Claim path ──────────────────────────────────────────────────────
            // The student was added by a teacher (ghost) and given a StudentCode.
            // Claiming is keyed by that code — NOT the phone — so nobody can take over
            // the account just by knowing/guessing the number.
            if (!string.IsNullOrWhiteSpace(request.StudentCode))
            {
                var code = request.StudentCode.Trim();
                var ghost = await _userManager.Users
                    .Include(u => u.StudentProfile)
                    .FirstOrDefaultAsync(u => u.StudentCode == code && u.IsGhostAccount, ct);

                if (ghost == null)
                    throw new ConflictException(["Invalid student code."]);

                // The phone they're signing up with must not belong to another account.
                var phoneTaken = await _userManager.Users
                    .AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != ghost.Id, ct);
                if (phoneTaken)
                    throw new ConflictException(["This phone number is already registered."]);

                ghost.FirstName = request.FirstName;
                ghost.LastName = request.LastName;
                ghost.Email = request.Email;
                ghost.UserName = request.PhoneNumber;
                ghost.PhoneNumber = request.PhoneNumber;
                ghost.IsGhostAccount = false;
                ghost.StudentCode = null; // consumed
                ghost.EmailConfirmed = false;

                if (ghost.StudentProfile != null)
                {
                    ghost.StudentProfile.EducationalStage = request.EducationalStage;
                    ghost.StudentProfile.GradeYear = request.GradeYear;
                }
                else
                {
                    ghost.StudentProfile = new StudentProfile
                    {
                        EducationalStage = request.EducationalStage,
                        GradeYear = request.GradeYear
                    };
                }

                var claimResetToken = await _userManager.GeneratePasswordResetTokenAsync(ghost);
                await _userManager.ResetPasswordAsync(ghost, claimResetToken, request.Password);

                return ghost.Id;
            }

            // ── Normal new registration ─────────────────────────────────────────
            var existingUserByPhone = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, ct);

            if (existingUserByPhone != null)
            {
                if (existingUserByPhone.EmailConfirmed)
                    throw new ConflictException(["This phone number is already registered and verified."]);

                throw new ConflictException(["This phone number is already registered. Please verify your email or reset your password."]);
            }

            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.PhoneNumber,
                PhoneNumber = request.PhoneNumber,
                UserType = UserType.Student,
                IsActive = true,
                IsGhostAccount = false,
                StudentProfile = new StudentProfile
                {
                    EducationalStage = request.EducationalStage,
                    GradeYear = request.GradeYear
                }
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded) throw new IdentityException(result.Errors.Select(e => e.Description).ToList());
            await _userManager.AddToRoleAsync(user, RoleConstants.Student);

            return user.Id;
        }

        // ══════════════════════════════════════════════════
        // Register Parent (Global Level)
        // ══════════════════════════════════════════════════
        public async Task<string> RegisterParentAsync(RegisterParentRequest request, CancellationToken ct = default)
        {
            var existingUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email || u.PhoneNumber == request.PhoneNumber, ct);

            if (existingUser != null)
            {
                if (existingUser.EmailConfirmed)
                {
                    var errors = new List<string>();
                    if (existingUser.Email == request.Email) errors.Add("This email is already registered and verified.");
                    if (existingUser.PhoneNumber == request.PhoneNumber) errors.Add("This phone number is already registered and verified.");
                    throw new ConflictException(errors);
                }

                existingUser.FirstName = request.FirstName;
                existingUser.LastName = request.LastName;
                existingUser.Email = request.Email;
                existingUser.UserName = request.PhoneNumber;
                existingUser.PhoneNumber = request.PhoneNumber;
                existingUser.EmailConfirmed = false;

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                await _userManager.ResetPasswordAsync(existingUser, resetToken, request.Password);

                return existingUser.Id;
            }

            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.PhoneNumber,
                PhoneNumber = request.PhoneNumber,
                UserType = UserType.Parent,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded) throw new IdentityException(result.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(user, RoleConstants.Parent);

            return user.Id;
        }

        // ══════════════════════════════════════════════════
        // Register Teacher (Creates Global User + Inactive Tenant)
        // ══════════════════════════════════════════════════
        public async Task<string> RegisterTeacherAsync(RegisterTeacherRequest request, CancellationToken ct = default)
        {
            var existingUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email || u.PhoneNumber == request.PhoneNumber, ct);

            if (existingUser != null)
            {
                if (existingUser.EmailConfirmed)
                {
                    var errors = new List<string>();
                    if (existingUser.Email == request.Email) errors.Add("This email is already registered and verified.");
                    if (existingUser.PhoneNumber == request.PhoneNumber) errors.Add("This phone number is already registered and verified.");
                    throw new ConflictException(errors);
                }

                existingUser.FirstName = request.FirstName;
                existingUser.LastName = request.LastName;
                existingUser.Email = request.Email;
                existingUser.UserName = request.PhoneNumber;
                existingUser.PhoneNumber = request.PhoneNumber;
                existingUser.EmailConfirmed = false;

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                await _userManager.ResetPasswordAsync(existingUser, resetToken, request.Password);

                return existingUser.Id;
            }

            var teacherUser = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.PhoneNumber,
                PhoneNumber = request.PhoneNumber,
                UserType = UserType.Teacher,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(teacherUser, request.Password);
            if (!result.Succeeded) throw new IdentityException(result.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(teacherUser, RoleConstants.Teacher);

            string tenantIdentifier = $"tenant_{request.PhoneNumber}";
            var createTenantRequest = new CreateTenantRequest
            {
                Identifier = tenantIdentifier,
                Name = $"Mr/Ms {request.FirstName} {request.LastName}'s Workspace",
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ValidUpTo = DateTime.UtcNow.AddMonths(1),
                ConnectionString = null
            };

            var newTenantId = await _tenantService.CreateTenantAsync(createTenantRequest, ct);
            await _tenantService.DeactivateTenantAsync(newTenantId, ct);
            await _userManager.AddClaimAsync(teacherUser, new Claim(ClaimConstants.Tenant, newTenantId));

            // Workspace membership is the source of truth for login/workspace selection.
            // Kept in sync with the tenant claim above (legacy paths still read the claim).
            _dbContext.WorkspaceMembers.Add(new WorkspaceMember
            {
                UserId = teacherUser.Id,
                TenantId = newTenantId,
                Role = WorkspaceRole.Owner,
                Status = WorkspaceMemberStatus.Active,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(ct);

            return teacherUser.Id;
        }

        // ══════════════════════════════════════════════════
        // Register Center (standalone center-owner account)
        // ══════════════════════════════════════════════════
        // Mirrors teacher registration but creates a CenterOwner account (never a teacher)
        // bound to a Center-type tenant. The tenant is created INACTIVE — it stays inactive
        // until the subscription is activated (admin today; self-service payment later).
        public async Task<string> RegisterCenterAsync(RegisterCenterRequest request, CancellationToken ct = default)
        {
            var existingUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email || u.PhoneNumber == request.PhoneNumber, ct);

            if (existingUser != null)
            {
                if (existingUser.EmailConfirmed)
                {
                    var errors = new List<string>();
                    if (existingUser.Email == request.Email) errors.Add("This email is already registered and verified.");
                    if (existingUser.PhoneNumber == request.PhoneNumber) errors.Add("This phone number is already registered and verified.");
                    throw new ConflictException(errors);
                }

                existingUser.FirstName = request.FirstName;
                existingUser.LastName = request.LastName;
                existingUser.Email = request.Email;
                existingUser.UserName = request.PhoneNumber;
                existingUser.PhoneNumber = request.PhoneNumber;
                existingUser.EmailConfirmed = false;

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                await _userManager.ResetPasswordAsync(existingUser, resetToken, request.Password);

                return existingUser.Id;
            }

            var ownerUser = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.PhoneNumber,
                PhoneNumber = request.PhoneNumber,
                UserType = UserType.CenterOwner,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(ownerUser, request.Password);
            if (!result.Succeeded) throw new IdentityException(result.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(ownerUser, RoleConstants.CenterOwner);

            // Center-type tenant. TEMPORARY: every new center gets an immediate 1-month
            // active trial (no seat limit) so it's usable straight away — mirrors the
            // teacher's one-month grant. This stands in until the self-service subscription
            // page exists; then registration will create it inactive and payment activates it.
            var tenantId = $"center_{Guid.NewGuid():N}";
            var tenant = new AppTenantInfo
            {
                Id = tenantId,
                Identifier = tenantId,
                Name = request.CenterName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ConnectionString = null,                      // shared DB, tenant-filtered
                ValidUpTo = DateTime.UtcNow.AddMonths(1),      // 1-month trial
                IsActive = true,
                Type = TenantType.Center,
                MaxTeachers = null                             // unlimited during the trial
            };
            await _tenantStore.TryAddAsync(tenant);

            await _userManager.AddClaimAsync(ownerUser, new Claim(ClaimConstants.Tenant, tenantId));

            // Workspace membership is the source of truth for login/workspace selection.
            // The owner gets full operational capability so they can run the center
            // (groups, attendance, payments) the moment they log in.
            _dbContext.WorkspaceMembers.Add(new WorkspaceMember
            {
                UserId = ownerUser.Id,
                TenantId = tenantId,
                Role = WorkspaceRole.Owner,
                Status = WorkspaceMemberStatus.Active,
                Permissions = CenterPermissions.All,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(ct);

            return ownerUser.Id;
        }

        // ══════════════════════════════════════════════════
        // Generate & Send OTP
        // ══════════════════════════════════════════════════
        public async Task GenerateAndSendOtpAsync(string email, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new NotFoundException(["User not found with this email."]);

            if (user.EmailConfirmed)
                throw new ConflictException(["Email is already verified."]);

            var lastOtp = await _dbContext.EmailVerifications
                .Where(e => e.UserId == user.Id && e.Purpose == VerificationPurpose.EmailConfirmation)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (lastOtp != null)
            {
                var timeSinceLastOtp = DateTime.UtcNow - lastOtp.CreatedAt;
                if (timeSinceLastOtp.TotalSeconds < 60)
                {
                    var remainingSeconds = 60 - (int)timeSinceLastOtp.TotalSeconds;
                    throw new ConflictException([$"Please wait {remainingSeconds} seconds before requesting a new OTP."]);
                }
            }

            string plainOtp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            string hashedOtp = HashOtp(plainOtp);

            var existingOtps = await _dbContext.EmailVerifications
                .Where(e => e.UserId == user.Id && !e.IsUsed && e.Purpose == VerificationPurpose.EmailConfirmation)
                .ToListAsync(ct);

            foreach (var existing in existingOtps)
            {
                existing.IsUsed = true;
            }

            var emailVerification = new EmailVerification
            {
                UserId = user.Id,
                OtpCode = hashedOtp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false,
                Purpose = VerificationPurpose.EmailConfirmation
            };

            await _dbContext.EmailVerifications.AddAsync(emailVerification, ct);
            await _dbContext.SaveChangesAsync(ct);

            string subject = "Faheem - Verify Your Email";
            string htmlBody = GetPremiumEmailTemplate(
                title: "Email Verification",
                message: "Welcome to Faheem! Please use the verification code below to confirm your email address and activate your account.",
                otp: plainOtp,
                validity: "30 minutes");

            await _emailService.SendEmailAsync(email, subject, htmlBody, ct);
        }

        // ══════════════════════════════════════════════════
        // Verify OTP
        // ══════════════════════════════════════════════════
        public async Task<string> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                throw new NotFoundException(["User not found."]);

            string hashedInputOtp = HashOtp(request.OtpCode);

            var verificationRecord = await _dbContext.EmailVerifications
                .Where(e => e.UserId == user.Id
                         && e.OtpCode == hashedInputOtp
                         && e.Purpose == VerificationPurpose.EmailConfirmation
                         && !e.IsUsed)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (verificationRecord == null)
                throw new ConflictException(["Invalid OTP code."]);

            if (verificationRecord.ExpiresAt < DateTime.UtcNow)
                throw new ConflictException(["OTP code has expired. Please request a new one."]);

            verificationRecord.IsUsed = true;
            user.EmailConfirmed = true;

            await _userManager.UpdateAsync(user);

            if (await _userManager.IsInRoleAsync(user, RoleConstants.Teacher))
            {
                var claims = await _userManager.GetClaimsAsync(user);
                var tenantId = claims.FirstOrDefault(c => c.Type == ClaimConstants.Tenant)?.Value;
                if (tenantId != null)
                    await _tenantService.ActivateTenantAsync(tenantId, ct);
            }

            await _dbContext.SaveChangesAsync(ct);

            return user.Id;
        }

        // ══════════════════════════════════════════════════
        // Forgot Password (Send OTP)
        // ══════════════════════════════════════════════════
        public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return;
            }

            var lastOtp = await _dbContext.EmailVerifications
                .Where(e => e.UserId == user.Id && e.Purpose == VerificationPurpose.PasswordReset)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (lastOtp != null)
            {
                var timeSinceLastOtp = DateTime.UtcNow - lastOtp.CreatedAt;
                if (timeSinceLastOtp.TotalSeconds < 60)
                {
                    return;
                }
            }

            string plainOtp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            string hashedOtp = HashOtp(plainOtp);

            var existingOtps = await _dbContext.EmailVerifications
                .Where(e => e.UserId == user.Id && !e.IsUsed && e.Purpose == VerificationPurpose.PasswordReset)
                .ToListAsync(ct);

            foreach (var existing in existingOtps)
            {
                existing.IsUsed = true;
            }

            var emailVerification = new EmailVerification
            {
                UserId = user.Id,
                OtpCode = hashedOtp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                IsUsed = false,
                Purpose = VerificationPurpose.PasswordReset
            };

            await _dbContext.EmailVerifications.AddAsync(emailVerification, ct);
            await _dbContext.SaveChangesAsync(ct);

            string subject = "Faheem - Password Reset Request";
            string htmlBody = GetPremiumEmailTemplate(
                title: "Reset Your Password",
                message: "We received a request to reset your password. Enter the code below to choose a new password.",
                otp: plainOtp,
                validity: "60 minutes");

            await _emailService.SendEmailAsync(email, subject, htmlBody, ct);
        }

        // ══════════════════════════════════════════════════
        // Reset Password
        // ══════════════════════════════════════════════════
        public async Task<string> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                throw new NotFoundException(["User not found."]);

            string hashedInputOtp = HashOtp(request.OtpCode);

            var verificationRecord = await _dbContext.EmailVerifications
                .Where(e => e.UserId == user.Id
                         && e.OtpCode == hashedInputOtp
                         && e.Purpose == VerificationPurpose.PasswordReset
                         && !e.IsUsed)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (verificationRecord == null)
                throw new ConflictException(["Invalid reset code."]);

            if (verificationRecord.ExpiresAt < DateTime.UtcNow)
                throw new ConflictException(["Reset code has expired. Please request a new one."]);

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

            if (!resetResult.Succeeded)
                throw new IdentityException(resetResult.Errors.Select(e => e.Description).ToList());

            await _userManager.UpdateSecurityStampAsync(user);

            var activeTokens = await _dbContext.UserRefreshTokens
                .Where(t => t.UserId == user.Id && !t.IsRevoked)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
            }

            verificationRecord.IsUsed = true;
            await _dbContext.SaveChangesAsync(ct);

            return user.Id;
        }

        // ══════════════════════════════════════════════════
        // Register Assistant
        // ══════════════════════════════════════════════════
        public async Task<string> RegisterAssistantAsync(RegisterAssistantRequest request, string teacherTenantId, CancellationToken ct = default)
        {
            // A teacher's assistant (secretary) is a personal-workspace concept: it manages
            // the teacher's own individual workspace only. Staff inside a center is the
            // owner's job (center Staff), so refuse to attach an assistant to a center.
            var tenant = await _tenantStore.TryGetByIdentifierAsync(teacherTenantId);
            if (tenant?.Type == TenantType.Center)
                throw new ConflictException(["السكرتيرة بتتعمل من مساحتك الشخصية، مش من داخل السنتر."]);

            var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == request.Email || u.PhoneNumber == request.PhoneNumber, ct);
            if (existingUser != null) throw new ConflictException(["This email or phone number is already registered."]);

            var assistantUser = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.PhoneNumber,
                PhoneNumber = request.PhoneNumber,
                UserType = UserType.Teacher,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(assistantUser, request.Password);
            if (!result.Succeeded) throw new IdentityException(result.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(assistantUser, RoleConstants.Assistant);
            await _userManager.AddClaimAsync(assistantUser, new Claim(ClaimConstants.Tenant, teacherTenantId));

            // Mirror the claim as a workspace membership (Assistant role in the teacher's
            // workspace). Capability comes from the teacher-granted flags — the Assistant
            // identity role itself has no base permissions.
            _dbContext.WorkspaceMembers.Add(new WorkspaceMember
            {
                UserId = assistantUser.Id,
                TenantId = teacherTenantId,
                Role = WorkspaceRole.Assistant,
                Status = WorkspaceMemberStatus.Active,
                Permissions = (CenterPermissions)request.Permissions,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(ct);

            return assistantUser.Id;
        }

        // ══════════════════════════════════════════════════
        // Assistant — List
        // ══════════════════════════════════════════════════
        public async Task<List<AssistantDto>> GetTeacherAssistantsAsync(string teacherTenantId, CancellationToken ct = default)
        {
            // Find users that carry the Tenant claim for this teacher's workspace.
            var assistantUserIds = await _dbContext.UserClaims
                .Where(uc => uc.ClaimType == ClaimConstants.Tenant && uc.ClaimValue == teacherTenantId)
                .Select(uc => uc.UserId)
                .ToListAsync(ct);

            if (assistantUserIds.Count == 0) return [];

            // Of those, keep only Assistants (exclude the owning teacher).
            var assistantRoleId = await _dbContext.Roles
                .Where(r => r.Name == RoleConstants.Assistant)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            if (assistantRoleId == null) return [];

            var assistantsInRole = await _dbContext.UserRoles
                .Where(ur => assistantUserIds.Contains(ur.UserId) && ur.RoleId == assistantRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync(ct);

            if (assistantsInRole.Count == 0) return [];

            var users = await _dbContext.Users
                .Where(u => assistantsInRole.Contains(u.Id))
                .Select(u => new AssistantDto
                {
                    Id          = u.Id,
                    FirstName   = u.FirstName,
                    LastName    = u.LastName,
                    Email       = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    IsActive    = u.IsActive
                })
                .ToListAsync(ct);

            // Attach each assistant's capability flags from their workspace membership.
            var permsByUser = await _dbContext.WorkspaceMembers
                .Where(m => m.TenantId == teacherTenantId && assistantsInRole.Contains(m.UserId))
                .ToDictionaryAsync(m => m.UserId, m => (int)m.Permissions, ct);
            foreach (var u in users) u.Permissions = permsByUser.GetValueOrDefault(u.Id);

            return users.OrderBy(a => a.FirstName).ToList();
        }

        // ══════════════════════════════════════════════════
        // Assistant — Set Permissions
        // ══════════════════════════════════════════════════
        public async Task<string> SetAssistantPermissionsAsync(string assistantUserId, string teacherTenantId, int permissions, CancellationToken ct = default)
        {
            var membership = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.UserId == assistantUserId
                                       && m.TenantId == teacherTenantId
                                       && m.Role == WorkspaceRole.Assistant, ct)
                ?? throw new NotFoundException(["المساعد ده مش تابع لمساحتك."]);

            membership.Permissions = (CenterPermissions)permissions;
            await _dbContext.SaveChangesAsync(ct);

            return "تم تحديث صلاحيات المساعد.";
        }

        // ══════════════════════════════════════════════════
        // Assistant — Remove
        // ══════════════════════════════════════════════════
        public async Task<string> RemoveAssistantAsync(string assistantUserId, string teacherTenantId, CancellationToken ct = default)
        {
            var assistant = await _userManager.FindByIdAsync(assistantUserId)
                ?? throw new NotFoundException(["Assistant not found."]);

            // Make sure the assistant actually belongs to this teacher's workspace.
            var hasTenantClaim = await _dbContext.UserClaims
                .AnyAsync(uc => uc.UserId == assistantUserId
                             && uc.ClaimType == ClaimConstants.Tenant
                             && uc.ClaimValue == teacherTenantId, ct);

            if (!hasTenantClaim)
                throw new ForbiddenException(["This assistant doesn't belong to your workspace."]);

            // Verify it's an assistant role — don't allow the teacher to remove themselves accidentally.
            var isAssistant = await _userManager.IsInRoleAsync(assistant, RoleConstants.Assistant);
            if (!isAssistant)
                throw new ConflictException(["This user is not an assistant."]);

            // Soft-remove: deactivate the user + strip the tenant claim.
            // Hard-deleting the user would break audit trails (e.g. PaidBy on PaymentTransactions).
            assistant.IsActive = false;
            await _userManager.UpdateAsync(assistant);

            var tenantClaims = await _dbContext.UserClaims
                .Where(uc => uc.UserId == assistantUserId
                          && uc.ClaimType == ClaimConstants.Tenant
                          && uc.ClaimValue == teacherTenantId)
                .ToListAsync(ct);

            _dbContext.UserClaims.RemoveRange(tenantClaims);

            // Drop the matching workspace membership so they lose workspace access too.
            var memberships = await _dbContext.WorkspaceMembers
                .Where(m => m.UserId == assistantUserId && m.TenantId == teacherTenantId)
                .ToListAsync(ct);

            _dbContext.WorkspaceMembers.RemoveRange(memberships);

            // Revoke any active refresh tokens so they can't keep using the app.
            var activeTokens = await _dbContext.UserRefreshTokens
                .Where(t => t.UserId == assistantUserId && !t.IsRevoked)
                .ToListAsync(ct);
            foreach (var t in activeTokens) t.IsRevoked = true;

            await _dbContext.SaveChangesAsync(ct);

            return "Assistant removed successfully.";
        }

        // ══════════════════════════════════════════════════
        // Profile — Get
        // ══════════════════════════════════════════════════
        public async Task<ProfileResponseDto> GetProfileAsync(string userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException(["User not found."]);

            var dto = new ProfileResponseDto
            {
                Id          = user.Id,
                FirstName   = user.FirstName,
                LastName    = user.LastName,
                Email       = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                UserType    = user.UserType.ToString(),
            };

            // Resolve the workspace + subscription for the CURRENTLY-SELECTED workspace.
            // Read the tenant from the live JWT (current workspace) — NOT the persisted
            // user claim, which always points at the user's original/individual tenant and
            // would show the wrong subscription when they're inside a center workspace.
            var tenantId = _currentUserService.TenantId;
            if (string.IsNullOrEmpty(tenantId))
            {
                var claims = await _userManager.GetClaimsAsync(user);
                tenantId = claims.FirstOrDefault(c => c.Type == ClaimConstants.Tenant)?.Value;
            }

            if (!string.IsNullOrEmpty(tenantId))
            {
                var tenantInfo = await _tenantService.GetTenantByIdAsync(tenantId, ct);
                if (tenantInfo != null)
                {
                    dto.TenantName             = tenantInfo.Name;
                    dto.SubscriptionValidUntil = tenantInfo.ValidUpTo;
                    dto.TenantIsActive         = tenantInfo.IsActive;
                }
            }

            return dto;
        }

        // ══════════════════════════════════════════════════
        // Profile — Update
        // ══════════════════════════════════════════════════
        public async Task<string> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException(["User not found."]);

            user.FirstName = request.FirstName.Trim();
            user.LastName  = request.LastName.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new IdentityException(result.Errors.Select(e => e.Description).ToList());

            return "Profile updated successfully.";
        }

        // ══════════════════════════════════════════════════
        // Change Password
        // ══════════════════════════════════════════════════
        public async Task<string> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException(["User not found."]);

            if (request.NewPassword != request.ConfirmNewPassword)
                throw new ConflictException(["New password and confirmation do not match."]);

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                throw new IdentityException(result.Errors.Select(e => e.Description).ToList());

            // Invalidate all existing refresh tokens so other devices re-login
            var activeTokens = await _dbContext.UserRefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
                token.IsRevoked = true;

            await _dbContext.SaveChangesAsync(ct);

            return "Password changed successfully.";
        }

        // ══════════════════════════════════════════════════
        // Self-Delete Account
        // ══════════════════════════════════════════════════
        public async Task<string> DeleteMyAccountAsync(string userId, CancellationToken ct = default)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                ?? throw new NotFoundException(["User not found."]);

            var roles = await _userManager.GetRolesAsync(user);

            // Teacher self-delete is gated by ownership — a single careless tap
            // shouldn't strand groups and students with no owner. They must wind
            // down their account through a teacher's manual path instead.
            // Tenant id is stored as a user claim (Finbuckle MultiTenant), not
            // a column on ApplicationUser, so we pull it from there.
            if (roles.Contains(RoleConstants.Teacher))
            {
                var userClaims = await _userManager.GetClaimsAsync(user);
                var teacherTenantId = userClaims.FirstOrDefault(c => c.Type == ClaimConstants.Tenant)?.Value;
                if (!string.IsNullOrEmpty(teacherTenantId))
                {
                    var ownsActiveGroups = await _dbContext.Groups
                        .AnyAsync(g => g.TenantId == teacherTenantId, ct);
                    if (ownsActiveGroups)
                        throw new ConflictException([
                            "Cannot delete: your workspace still has active groups. Delete them first."
                        ]);
                }
            }

            // Remove everything that points at this user before the Identity delete.
            // Some relationships use DeleteBehavior.Restrict on purpose — their tables
            // already cascade from another path (e.g. Group → GroupStudent) and SQL
            // Server forbids multiple cascade paths — so without clearing them first the
            // user delete fails with a foreign-key violation (the 500 we saw on
            // DELETE /api/profile). ExecuteDelete + IgnoreQueryFilters issues direct
            // DELETEs that span tenants (a student's rows live under the teacher's
            // tenant) and don't trip the multi-tenant change-tracking guard. Everything
            // runs in one transaction so a failure rolls the whole thing back.
            await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

            // Multi-tenant rows — bypass the tenant filter to catch them all.
            await _dbContext.GroupStudents.IgnoreQueryFilters()
                .Where(gs => gs.StudentId == userId).ExecuteDeleteAsync(ct);
            await _dbContext.AttendanceRecords.IgnoreQueryFilters()
                .Where(a => a.StudentId == userId).ExecuteDeleteAsync(ct);
            await _dbContext.StudentGrades.IgnoreQueryFilters()
                .Where(g => g.StudentId == userId).ExecuteDeleteAsync(ct);
            await _dbContext.StudentPaymentRecords.IgnoreQueryFilters()
                .Where(p => p.StudentId == userId).ExecuteDeleteAsync(ct);
            await _dbContext.Notifications.IgnoreQueryFilters()
                .Where(n => n.UserId == userId).ExecuteDeleteAsync(ct);

            // Not multi-tenant.
            await _dbContext.ParentStudentLinks
                .Where(l => l.ParentUserId == userId || l.StudentUserId == userId)
                .ExecuteDeleteAsync(ct);
            await _dbContext.UserDevices
                .Where(d => d.UserId == userId).ExecuteDeleteAsync(ct);
            await _dbContext.UserRefreshTokens
                .Where(t => t.UserId == userId).ExecuteDeleteAsync(ct);

            // Identity user — StudentProfile + EmailVerifications cascade automatically.
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new IdentityException(result.Errors.Select(e => e.Description).ToList());

            await tx.CommitAsync(ct);

            return "Account deleted.";
        }

        // ══════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════
        private static string HashOtp(string otp)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(otp);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // ══════════════════════════════════════════════════
        // Email HTML Template Builder
        // ══════════════════════════════════════════════════
        private string GetPremiumEmailTemplate(string title, string message, string otp, string validity)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            </head>
            <body style='margin: 0; padding: 0; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7f6;'>
                <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color: #f4f7f6; padding: 40px 20px;'>
                    <tr>
                        <td align='center'>
                            <table width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width: 600px; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.05); overflow: hidden;'>
                                <!-- Header -->
                                <tr>
                                    <td align='center' style='padding: 40px 0 20px 0;'>
                                        <h1 style='margin: 0; color: #0f172a; font-size: 32px; font-weight: 800; letter-spacing: 2px;'>FAHEEM</h1>
                                        <p style='margin: 5px 0 0 0; color: #64748b; font-size: 14px; letter-spacing: 1px;'>PREMIUM EDUCATION PLATFORM</p>
                                    </td>
                                </tr>
                                <!-- Content -->
                                <tr>
                                    <td style='padding: 20px 40px; text-align: center;'>
                                        <h2 style='margin: 0 0 15px 0; color: #334155; font-size: 20px; font-weight: 600;'>{title}</h2>
                                        <p style='margin: 0 0 25px 0; color: #475569; font-size: 16px; line-height: 1.6;'>
                                            {message}
                                        </p>
                                        <!-- OTP Box -->
                                        <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; margin: 0 auto; max-width: 300px;'>
                                            <span style='font-family: monospace; font-size: 36px; font-weight: 700; color: #2563eb; letter-spacing: 8px;'>{otp}</span>
                                        </div>
                                        <p style='margin: 25px 0 0 0; color: #64748b; font-size: 14px;'>
                                            This code is valid for <strong>{validity}</strong>.
                                        </p>
                                    </td>
                                </tr>
                                <!-- Footer -->
                                <tr>
                                    <td style='padding: 30px 40px; background-color: #f8fafc; text-align: center; border-top: 1px solid #f1f5f9;'>
                                        <p style='margin: 0; color: #94a3b8; font-size: 12px; line-height: 1.5;'>
                                            If you didn't request this email, you can safely ignore it.<br>
                                            &copy; {DateTime.Now.Year} Faheem Platform. All rights reserved.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>";
        }
    }
}