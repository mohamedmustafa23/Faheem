using Application.Exceptions;
using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Constants;
using Infrastructure.Contexts;
using Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Tenancy
{
    public class CenterService : ICenterService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;

        public CenterService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IMultiTenantStore<AppTenantInfo> tenantStore)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _tenantStore = tenantStore;
        }

        // ── Center owner: invite an existing user as a teacher ─────────────────
        public async Task<string> InviteTeacherAsync(string tenantId, string ownerUserId, InviteTeacherRequest request, CancellationToken ct = default)
        {
            await EnsureOwnerAsync(tenantId, ownerUserId, ct);

            var tenant = await _tenantStore.TryGetAsync(tenantId)
                ?? throw new NotFoundException(["Center not found."]);
            if (tenant.Type != TenantType.Center)
                throw new ConflictException(["This workspace is not a center."]);
            if (!tenant.IsActive || tenant.ValidUpTo < DateTime.UtcNow)
                throw new ConflictException(["The center subscription is inactive. Activate it before adding teachers."]);

            var key = request.PhoneOrEmail.Trim();
            var invitee = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == key || u.Email == key, ct)
                ?? throw new NotFoundException(["No registered user found with that phone/email. Ask them to create an account first."]);

            if (!invitee.EmailConfirmed)
                throw new ConflictException(["This user hasn't verified their account yet."]);

            var existing = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.UserId == invitee.Id && m.TenantId == tenantId, ct);
            if (existing != null)
                throw new ConflictException([existing.Status == WorkspaceMemberStatus.Invited
                    ? "This user already has a pending invite to the center."
                    : "This user is already a member of the center."]);

            // Seat limit — only member teachers count (the owner doesn't).
            if (tenant.MaxTeachers.HasValue)
            {
                var teacherCount = await _dbContext.WorkspaceMembers
                    .CountAsync(m => m.TenantId == tenantId && m.Role == WorkspaceRole.Teacher, ct);
                if (teacherCount >= tenant.MaxTeachers.Value)
                    throw new ConflictException([$"Seat limit reached ({tenant.MaxTeachers} teachers). Upgrade the package to add more."]);
            }

            _dbContext.WorkspaceMembers.Add(new WorkspaceMember
            {
                UserId = invitee.Id,
                TenantId = tenantId,
                Role = WorkspaceRole.Teacher,
                Status = WorkspaceMemberStatus.Invited,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(ct);

            return "Invitation sent.";
        }

        // ── Invited user: accept or decline ────────────────────────────────────
        public async Task<string> RespondToInviteAsync(string userId, string tenantId, bool accept, CancellationToken ct = default)
        {
            var membership = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.UserId == userId
                                       && m.TenantId == tenantId
                                       && m.Status == WorkspaceMemberStatus.Invited, ct)
                ?? throw new NotFoundException(["No pending invite found for this center."]);

            if (!accept)
            {
                _dbContext.WorkspaceMembers.Remove(membership);
                await _dbContext.SaveChangesAsync(ct);
                return "Invitation declined.";
            }

            membership.Status = WorkspaceMemberStatus.Active;
            await _dbContext.SaveChangesAsync(ct);

            // Keep the tenant claim in sync (legacy resolution paths still read it).
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var claims = await _userManager.GetClaimsAsync(user);
                if (!claims.Any(c => c.Type == ClaimConstants.Tenant && c.Value == tenantId))
                    await _userManager.AddClaimAsync(user, new Claim(ClaimConstants.Tenant, tenantId));
            }

            return "Invitation accepted.";
        }

        public async Task<List<PendingInviteDto>> GetMyInvitesAsync(string userId, CancellationToken ct = default)
        {
            var invites = await _dbContext.WorkspaceMembers
                .Where(m => m.UserId == userId && m.Status == WorkspaceMemberStatus.Invited)
                .ToListAsync(ct);

            var result = new List<PendingInviteDto>();
            foreach (var m in invites)
            {
                var t = await _tenantStore.TryGetAsync(m.TenantId);
                result.Add(new PendingInviteDto
                {
                    TenantId = m.TenantId,
                    CenterName = t?.Name ?? m.TenantId,
                    InvitedAt = m.CreatedAt
                });
            }
            return result;
        }

        public async Task<List<CenterMemberDto>> GetCenterMembersAsync(string tenantId, string ownerUserId, CancellationToken ct = default)
        {
            await EnsureOwnerAsync(tenantId, ownerUserId, ct);

            var members = await _dbContext.WorkspaceMembers
                .Where(m => m.TenantId == tenantId)
                .ToListAsync(ct);

            var userIds = members.Select(m => m.UserId).ToList();
            var users = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u, ct);

            return members.Select(m =>
            {
                users.TryGetValue(m.UserId, out var u);
                return new CenterMemberDto
                {
                    UserId = m.UserId,
                    FirstName = u?.FirstName ?? "",
                    LastName = u?.LastName ?? "",
                    PhoneNumber = u?.PhoneNumber ?? "",
                    Email = u?.Email ?? "",
                    Role = m.Role.ToString(),
                    Status = m.Status.ToString()
                };
            })
            .OrderBy(x => x.Role)
            .ThenBy(x => x.FirstName)
            .ToList();
        }

        public async Task<string> RemoveMemberAsync(string tenantId, string ownerUserId, string memberUserId, CancellationToken ct = default)
        {
            await EnsureOwnerAsync(tenantId, ownerUserId, ct);

            if (memberUserId == ownerUserId)
                throw new ConflictException(["The owner can't remove themselves from the center."]);

            var membership = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.UserId == memberUserId && m.TenantId == tenantId, ct)
                ?? throw new NotFoundException(["This user is not a member of the center."]);

            if (membership.Role == WorkspaceRole.Owner)
                throw new ConflictException(["Can't remove the center owner."]);

            _dbContext.WorkspaceMembers.Remove(membership);

            // Strip the tenant claim + revoke active sessions so access is lost immediately.
            var claims = await _dbContext.UserClaims
                .Where(uc => uc.UserId == memberUserId
                          && uc.ClaimType == ClaimConstants.Tenant
                          && uc.ClaimValue == tenantId)
                .ToListAsync(ct);
            _dbContext.UserClaims.RemoveRange(claims);

            var tokens = await _dbContext.UserRefreshTokens
                .Where(t => t.UserId == memberUserId && !t.IsRevoked)
                .ToListAsync(ct);
            foreach (var t in tokens) t.IsRevoked = true;

            await _dbContext.SaveChangesAsync(ct);

            return "Member removed from the center.";
        }

        public async Task<CenterOverviewDto> GetCenterOverviewAsync(string tenantId, string ownerUserId, CancellationToken ct = default)
        {
            await EnsureOwnerAsync(tenantId, ownerUserId, ct);

            var tenant = await _tenantStore.TryGetAsync(tenantId)
                ?? throw new NotFoundException(["Center not found."]);

            var activeTeachers = await _dbContext.WorkspaceMembers
                .CountAsync(m => m.TenantId == tenantId
                              && m.Role == WorkspaceRole.Teacher
                              && m.Status == WorkspaceMemberStatus.Active, ct);

            var pendingInvites = await _dbContext.WorkspaceMembers
                .CountAsync(m => m.TenantId == tenantId
                              && m.Status == WorkspaceMemberStatus.Invited, ct);

            var daysRemaining = (tenant.ValidUpTo.Date - DateTime.UtcNow.Date).Days;
            if (daysRemaining < 0) daysRemaining = 0;

            return new CenterOverviewDto
            {
                TenantId = tenantId,
                Name = tenant.Name ?? tenantId,
                IsActive = tenant.IsActive,
                SubscriptionValidUntil = tenant.ValidUpTo,
                SubscriptionDaysRemaining = daysRemaining,
                MaxTeachers = tenant.MaxTeachers,
                ActiveTeachers = activeTeachers,
                PendingInvites = pendingInvites
            };
        }

        // ── Admin / payment: activate or renew a center subscription ───────────
        public async Task<DateTime> SetCenterSubscriptionAsync(SetCenterSubscriptionRequest request, CancellationToken ct = default)
        {
            if (request.ExtendMonths < 1)
                throw new ConflictException(["Extension must be at least 1 month."]);

            var tenant = await _tenantStore.TryGetAsync(request.TenantId)
                ?? throw new NotFoundException(["Center not found."]);
            if (tenant.Type != TenantType.Center)
                throw new ConflictException(["This workspace is not a center."]);

            // Renewal math: if the subscription is still in the future, stack the extension on
            // top of the existing expiry so the remaining days aren't lost. If it has already
            // lapsed, start fresh from today (no credit for the days that passed unsubscribed).
            var now = DateTime.UtcNow;
            var basis = tenant.ValidUpTo > now ? tenant.ValidUpTo : now;
            tenant.ValidUpTo = basis.AddMonths(request.ExtendMonths);
            tenant.IsActive = true;
            tenant.MaxTeachers = request.MaxTeachers;

            await _tenantStore.TryUpdateAsync(tenant);
            return tenant.ValidUpTo;
        }

        // The caller must be an active Owner of the given center.
        private async Task EnsureOwnerAsync(string tenantId, string callerUserId, CancellationToken ct)
        {
            var isOwner = await _dbContext.WorkspaceMembers
                .AnyAsync(m => m.TenantId == tenantId
                            && m.UserId == callerUserId
                            && m.Role == WorkspaceRole.Owner
                            && m.Status == WorkspaceMemberStatus.Active, ct);
            if (!isOwner)
                throw new ForbiddenException(["Only the center owner can perform this action."]);
        }
    }
}
