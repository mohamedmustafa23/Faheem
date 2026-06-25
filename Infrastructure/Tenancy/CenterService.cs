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
                ?? throw new NotFoundException(["السنتر غير موجود."]);
            if (tenant.Type != TenantType.Center)
                throw new ConflictException(["مساحة العمل دي مش سنتر."]);
            if (!tenant.IsActive || tenant.ValidUpTo < DateTime.UtcNow)
                throw new ConflictException(["اشتراك السنتر غير مفعّل. فعّله قبل إضافة مدرّسين."]);

            var key = request.PhoneOrEmail.Trim();
            var invitee = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == key || u.Email == key, ct)
                ?? throw new NotFoundException(["مفيش مستخدم مسجّل بالرقم أو الإيميل ده. اطلب منه يعمل حساب الأول."]);

            if (!invitee.EmailConfirmed)
                throw new ConflictException(["المستخدم ده لسه مفعّلش حسابه."]);

            var existing = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.UserId == invitee.Id && m.TenantId == tenantId, ct);
            if (existing != null)
                throw new ConflictException([existing.Status == WorkspaceMemberStatus.Invited
                    ? "المستخدم ده عنده دعوة معلّقة للسنتر بالفعل."
                    : "المستخدم ده عضو في السنتر بالفعل."]);

            // Seat limit — only member teachers count (the owner doesn't).
            if (tenant.MaxTeachers.HasValue)
            {
                var teacherCount = await _dbContext.WorkspaceMembers
                    .CountAsync(m => m.TenantId == tenantId && m.Role == WorkspaceRole.Teacher, ct);
                if (teacherCount >= tenant.MaxTeachers.Value)
                    throw new ConflictException([$"وصلت للحد الأقصى ({tenant.MaxTeachers} مدرّسين). رقّي الباقة لإضافة المزيد."]);
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
                ?? throw new NotFoundException(["مفيش دعوة معلّقة للسنتر ده."]);

            if (!accept)
            {
                _dbContext.WorkspaceMembers.Remove(membership);
                await _dbContext.SaveChangesAsync(ct);
                return "تم رفض الدعوة.";
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

            return "تم قبول الدعوة.";
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
                throw new ConflictException(["المالك ميقدرش يشيل نفسه من السنتر."]);

            var membership = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.UserId == memberUserId && m.TenantId == tenantId, ct)
                ?? throw new NotFoundException(["المستخدم ده مش عضو في السنتر."]);

            if (membership.Role == WorkspaceRole.Owner)
                throw new ConflictException(["ميصحّش تشيل صاحب السنتر."]);

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

            return "تم إزالة العضو من السنتر.";
        }

        public async Task<CenterOverviewDto> GetCenterOverviewAsync(string tenantId, string ownerUserId, CancellationToken ct = default)
        {
            await EnsureOwnerAsync(tenantId, ownerUserId, ct);

            var tenant = await _tenantStore.TryGetAsync(tenantId)
                ?? throw new NotFoundException(["السنتر غير موجود."]);

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
                throw new ConflictException(["التمديد لازم يكون شهر على الأقل."]);

            var tenant = await _tenantStore.TryGetAsync(request.TenantId)
                ?? throw new NotFoundException(["السنتر غير موجود."]);
            if (tenant.Type != TenantType.Center)
                throw new ConflictException(["مساحة العمل دي مش سنتر."]);

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

        // ── Center owner: per-teacher revenue report ───────────────────────────
        public async Task<CenterFinancialsDto> GetCenterFinancialsAsync(string tenantId, string ownerUserId, CancellationToken ct = default)
        {
            await EnsureOwnerAsync(tenantId, ownerUserId, ct);

            // Active teacher members + their configured share.
            var teacherMembers = await _dbContext.WorkspaceMembers
                .Where(m => m.TenantId == tenantId
                         && m.Role == WorkspaceRole.Teacher
                         && m.Status == WorkspaceMemberStatus.Active)
                .Select(m => new { m.UserId, m.SharePercent })
                .ToListAsync(ct);

            var teacherIds = teacherMembers.Select(m => m.UserId).ToList();
            var names = await _dbContext.Users
                .Where(u => teacherIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
                .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

            // All groups in the center (owner's query filter already returns them all),
            // mapped to their owning teacher.
            var groups = await _dbContext.Groups
                .Where(g => g.TenantId == tenantId)
                .Select(g => new { g.Id, g.OwnerUserId })
                .ToListAsync(ct);
            var groupIds = groups.Select(g => g.Id).ToList();

            // One pull of every payment record with its collected total — same money
            // math as the teacher overview (Waived ⇒ expected 0; collected = Σ transactions).
            var records = groupIds.Count == 0
                ? new()
                : await _dbContext.StudentPaymentRecords
                    .Where(r => groupIds.Contains(r.GroupId))
                    .Select(r => new
                    {
                        r.GroupId,
                        r.StudentId,
                        r.Status,
                        r.ExpectedAmount,
                        r.DiscountAmount,
                        Paid = r.Transactions.Sum(t => (decimal?)t.Amount) ?? 0m
                    })
                    .ToListAsync(ct);

            var enrollments = groupIds.Count == 0
                ? new()
                : await _dbContext.GroupStudents
                    .Where(gs => groupIds.Contains(gs.GroupId))
                    .Select(gs => new { gs.GroupId, gs.StudentId })
                    .ToListAsync(ct);

            // Aggregate per group first.
            var collectedByGroup = new Dictionary<Guid, decimal>();
            var expectedByGroup = new Dictionary<Guid, decimal>();
            var outstandingByGroup = new Dictionary<Guid, HashSet<string>>();
            foreach (var r in records)
            {
                decimal expected = r.Status == PaymentStatus.Waived ? 0m : r.ExpectedAmount - r.DiscountAmount;
                collectedByGroup[r.GroupId] = collectedByGroup.GetValueOrDefault(r.GroupId) + r.Paid;
                expectedByGroup[r.GroupId] = expectedByGroup.GetValueOrDefault(r.GroupId) + expected;
                if (expected - r.Paid > 0)
                {
                    if (!outstandingByGroup.TryGetValue(r.GroupId, out var set))
                        outstandingByGroup[r.GroupId] = set = new HashSet<string>(StringComparer.Ordinal);
                    set.Add(r.StudentId);
                }
            }
            var studentsByGroup = enrollments.GroupBy(e => e.GroupId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.StudentId).Distinct().Count());

            var result = new CenterFinancialsDto { TeachersCount = teacherMembers.Count };

            foreach (var tm in teacherMembers)
            {
                var myGroupIds = groups.Where(g => g.OwnerUserId == tm.UserId).Select(g => g.Id).ToList();

                decimal collected = myGroupIds.Sum(id => collectedByGroup.GetValueOrDefault(id));
                decimal expected = myGroupIds.Sum(id => expectedByGroup.GetValueOrDefault(id));
                decimal remaining = Math.Max(0m, expected - collected);

                decimal sharePct = tm.SharePercent ?? 0m;
                decimal centerCut = Math.Round(collected * sharePct / 100m, 2);
                decimal teacherCut = collected - centerCut;

                result.Teachers.Add(new CenterTeacherFinancialDto
                {
                    TeacherId = tm.UserId,
                    TeacherName = names.GetValueOrDefault(tm.UserId, ""),
                    SharePercent = tm.SharePercent,
                    Collected = collected,
                    Expected = expected,
                    Remaining = remaining,
                    CenterCut = centerCut,
                    TeacherCut = teacherCut,
                    GroupsCount = myGroupIds.Count,
                    StudentsCount = myGroupIds.Sum(id => studentsByGroup.GetValueOrDefault(id)),
                    OutstandingStudentsCount = myGroupIds
                        .SelectMany(id => outstandingByGroup.GetValueOrDefault(id) ?? Enumerable.Empty<string>())
                        .Distinct().Count(),
                });

                result.TotalCollected += collected;
                result.TotalExpected += expected;
                result.CenterShareTotal += centerCut;
                result.TeachersShareTotal += teacherCut;
            }

            result.TotalRemaining = Math.Max(0m, result.TotalExpected - result.TotalCollected);
            result.Teachers = result.Teachers
                .OrderByDescending(t => t.Collected)
                .ThenBy(t => t.TeacherName)
                .ToList();

            return result;
        }

        // ── Center owner: one teacher's financial detail (drill-in + statement) ──
        public async Task<CenterTeacherDetailDto> GetCenterTeacherDetailAsync(string tenantId, string ownerUserId, string teacherUserId, CancellationToken ct = default)
        {
            await EnsureOwnerAsync(tenantId, ownerUserId, ct);

            var membership = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.TenantId == tenantId
                                       && m.UserId == teacherUserId
                                       && m.Role == WorkspaceRole.Teacher
                                       && m.Status == WorkspaceMemberStatus.Active, ct)
                ?? throw new NotFoundException(["المدرّس ده مش عضو في السنتر."]);

            var name = await _dbContext.Users
                .Where(u => u.Id == teacherUserId)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefaultAsync(ct) ?? "";

            var groups = await _dbContext.Groups
                .Where(g => g.TenantId == tenantId && g.OwnerUserId == teacherUserId)
                .Select(g => new { g.Id, g.Name, g.Subject, g.MonthlyFee })
                .ToListAsync(ct);
            var groupIds = groups.Select(g => g.Id).ToList();

            var records = groupIds.Count == 0
                ? new()
                : await _dbContext.StudentPaymentRecords
                    .Where(r => groupIds.Contains(r.GroupId))
                    .Select(r => new
                    {
                        r.GroupId,
                        r.StudentId,
                        r.Status,
                        r.ExpectedAmount,
                        r.DiscountAmount,
                        Paid = r.Transactions.Sum(t => (decimal?)t.Amount) ?? 0m
                    })
                    .ToListAsync(ct);

            var enrollments = groupIds.Count == 0
                ? new()
                : await _dbContext.GroupStudents
                    .Where(gs => groupIds.Contains(gs.GroupId))
                    .Select(gs => new { gs.GroupId, gs.StudentId })
                    .ToListAsync(ct);

            var rows = groups.ToDictionary(g => g.Id, g => new CenterTeacherGroupRow
            {
                GroupId = g.Id,
                GroupName = g.Name,
                Subject = g.Subject,
                MonthlyFee = g.MonthlyFee,
            });

            foreach (var gs in enrollments.GroupBy(e => e.GroupId))
                if (rows.TryGetValue(gs.Key, out var row))
                    row.StudentsCount = gs.Select(x => x.StudentId).Distinct().Count();

            var outstandingByGroup = new Dictionary<Guid, HashSet<string>>();
            foreach (var r in records)
            {
                decimal expected = r.Status == PaymentStatus.Waived ? 0m : r.ExpectedAmount - r.DiscountAmount;
                if (rows.TryGetValue(r.GroupId, out var row))
                {
                    row.Collected += r.Paid;
                    row.Expected += expected;
                }
                if (expected - r.Paid > 0)
                {
                    if (!outstandingByGroup.TryGetValue(r.GroupId, out var set))
                        outstandingByGroup[r.GroupId] = set = new HashSet<string>(StringComparer.Ordinal);
                    set.Add(r.StudentId);
                }
            }

            foreach (var row in rows.Values)
            {
                row.Remaining = Math.Max(0m, row.Expected - row.Collected);
                row.OutstandingStudentsCount = outstandingByGroup.TryGetValue(row.GroupId, out var set) ? set.Count : 0;
            }

            decimal collected = rows.Values.Sum(r => r.Collected);
            decimal expectedTotal = rows.Values.Sum(r => r.Expected);
            decimal sharePct = membership.SharePercent ?? 0m;
            decimal centerCut = Math.Round(collected * sharePct / 100m, 2);

            return new CenterTeacherDetailDto
            {
                TeacherId = teacherUserId,
                TeacherName = name,
                SharePercent = membership.SharePercent,
                Collected = collected,
                Expected = expectedTotal,
                Remaining = Math.Max(0m, expectedTotal - collected),
                CenterCut = centerCut,
                TeacherCut = collected - centerCut,
                Groups = rows.Values.OrderByDescending(r => r.Collected).ThenBy(r => r.GroupName).ToList(),
            };
        }

        // ── Center owner: set a teacher's revenue share ─────────────────────────
        public async Task<string> SetTeacherShareAsync(string tenantId, string ownerUserId, string teacherUserId, decimal? sharePercent, CancellationToken ct = default)
        {
            await EnsureOwnerAsync(tenantId, ownerUserId, ct);

            if (sharePercent.HasValue && (sharePercent.Value < 0 || sharePercent.Value > 100))
                throw new ConflictException(["النسبة لازم تكون بين 0 و100."]);

            var membership = await _dbContext.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.TenantId == tenantId
                                       && m.UserId == teacherUserId
                                       && m.Status == WorkspaceMemberStatus.Active, ct)
                ?? throw new NotFoundException(["المستخدم ده مش عضو في السنتر."]);

            if (membership.Role != WorkspaceRole.Teacher)
                throw new ConflictException(["النسبة بتتحدد للمدرّسين بس."]);

            membership.SharePercent = sharePercent;
            await _dbContext.SaveChangesAsync(ct);

            return "تم تحديث نسبة المدرّس.";
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
                throw new ForbiddenException(["صاحب السنتر بس اللي يقدر يعمل الإجراء ده."]);
        }
    }
}
