using Application.Exceptions;
using Application.Features.Tenancy.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Identity.Models;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenancy
{
    // Admin control-center data + subscription actions. Counts are read from the app
    // DB with IgnoreQueryFilters() because the admin is NOT tenant-scoped (the global
    // query filter would otherwise return nothing for a user with no tenant).
    public class AdminService : IAdminService
    {
        private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
        private readonly ApplicationDbContext _appDb;
        private readonly ISubscriptionService _subscriptions;

        public AdminService(IMultiTenantStore<AppTenantInfo> tenantStore, ApplicationDbContext appDb, ISubscriptionService subscriptions)
        {
            _tenantStore = tenantStore;
            _appDb = appDb;
            _subscriptions = subscriptions;
        }

        public async Task<List<AdminSubscriberDto>> GetSubscribersAsync(CancellationToken ct = default)
        {
            // The Root tenant is the system/admin account — never a managed subscriber.
            var tenants = (await _tenantStore.GetAllAsync())
                .Where(t => t.Id != TenancyConstants.Root.Id)
                .ToList();
            if (tenants.Count == 0) return new();

            var counts = await LoadCountsAsync(tenants, ct);
            return tenants
                .Select(t => Map(t, counts))
                // Soonest-to-expire first — that's the renewal pipeline the owner works.
                .OrderBy(s => s.ValidUpTo)
                .ToList();
        }

        public async Task<AdminSubscriberDto?> GetSubscriberByIdAsync(string id, CancellationToken ct = default)
        {
            var tenant = await _tenantStore.TryGetAsync(id);
            if (tenant is null) return null;

            var counts = await LoadCountsAsync(new List<AppTenantInfo> { tenant }, ct);
            return Map(tenant, counts);
        }

        public async Task<AdminSubscriberDto> ExtendSubscriptionAsync(string id, int months, int? maxTeachers = null, CancellationToken ct = default)
        {
            GuardNotRoot(id);
            if (months <= 0 || months > 36)
                throw new ConflictException(["مدة التجديد لازم تكون بين شهر و36 شهر."]);

            // The actual renewal lives in the shared subscription service — the same
            // path a payment webhook / self-serve renewal will use. For centers it also
            // sets the seat limit (seats × duration is one package).
            await _subscriptions.ExtendAsync(id, months, maxTeachers, ct);
            return (await GetSubscriberByIdAsync(id, ct))!;
        }

        public async Task<AdminSubscriberDto> SetActiveAsync(string id, bool isActive, CancellationToken ct = default)
        {
            GuardNotRoot(id);
            await _subscriptions.SetActiveAsync(id, isActive, ct);
            return (await GetSubscriberByIdAsync(id, ct))!;
        }

        public async Task<AdminSubscriberDto> SetCenterSeatsAsync(string id, int? maxTeachers, CancellationToken ct = default)
        {
            GuardNotRoot(id);
            if (maxTeachers is < 1)
                throw new ConflictException(["عدد المقاعد لازم يكون 1 على الأقل."]);

            var tenant = await _tenantStore.TryGetAsync(id)
                ?? throw new NotFoundException(["المشترك مش موجود."]);

            if (tenant.Type != TenantType.Center)
                throw new ConflictException(["المقاعد بتتظبط للسناتر بس."]);

            tenant.MaxTeachers = maxTeachers; // null = غير محدود
            await _tenantStore.TryUpdateAsync(tenant);
            return (await GetSubscriberByIdAsync(id, ct))!;
        }

        public async Task DeleteSubscriberAsync(string id, CancellationToken ct = default)
        {
            GuardNotRoot(id);
            var tenant = await _tenantStore.TryGetAsync(id)
                ?? throw new NotFoundException(["المشترك مش موجود."]);

            // Deleting a subscriber removes their WHOLE workspace — purge every
            // tenant-scoped row, children before parents, bypassing the tenant filter
            // (the admin isn't tenant-scoped). One transaction so a failure rolls back.
            await using var tx = await _appDb.Database.BeginTransactionAsync(ct);

            await _appDb.PaymentTransactions.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.StudentPaymentRecords.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.AttendanceRecords.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.LessonReportEntries.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.LessonReports.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.StudentGrades.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.Exams.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.SessionOccurrences.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.PaymentCycles.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.Sessions.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.GroupStudents.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.GroupAnnouncements.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.Materials.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.Groups.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.Notifications.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);
            await _appDb.WorkspaceMembers.IgnoreQueryFilters().Where(x => x.TenantId == id).ExecuteDeleteAsync(ct);

            await tx.CommitAsync(ct);

            await _tenantStore.TryRemoveAsync(tenant.Id);
        }

        // The Root tenant is the system/admin account — it must never be suspended,
        // renewed, or otherwise managed as a customer.
        private static void GuardNotRoot(string id)
        {
            if (id == TenancyConstants.Root.Id)
                throw new ForbiddenException(["مينفعش تتحكّم في حساب النظام."]);
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        private sealed record Counts(
            Dictionary<string, int> Groups,
            Dictionary<string, int> Students,
            Dictionary<string, int> Teachers,
            Dictionary<string, string> Phones);

        private async Task<Counts> LoadCountsAsync(List<AppTenantInfo> tenants, CancellationToken ct)
        {
            var tenantIds = tenants.Select(t => t.Id).ToList();

            var groups = (await _appDb.Groups.IgnoreQueryFilters()
                    .Where(g => tenantIds.Contains(g.TenantId))
                    .GroupBy(g => g.TenantId)
                    .Select(g => new { TenantId = g.Key, Count = g.Count() })
                    .ToListAsync(ct))
                .ToDictionary(x => x.TenantId, x => x.Count);

            var students = (await _appDb.GroupStudents.IgnoreQueryFilters()
                    .Where(gs => tenantIds.Contains(gs.TenantId))
                    .Select(gs => new { gs.TenantId, gs.StudentId })
                    .Distinct()
                    .ToListAsync(ct))
                .GroupBy(x => x.TenantId)
                .ToDictionary(g => g.Key, g => g.Count());

            var teachers = (await _appDb.WorkspaceMembers.IgnoreQueryFilters()
                    .Where(m => tenantIds.Contains(m.TenantId)
                                && m.Role == WorkspaceRole.Teacher
                                && m.Status == WorkspaceMemberStatus.Active)
                    .GroupBy(m => m.TenantId)
                    .Select(g => new { TenantId = g.Key, Count = g.Count() })
                    .ToListAsync(ct))
                .ToDictionary(x => x.TenantId, x => x.Count);

            // Owner phone: the tenant store keeps the owner email; the phone lives on
            // the ApplicationUser, so match by email.
            var emails = tenants.Select(t => t.Email).Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
            var phones = (await _appDb.Users.IgnoreQueryFilters()
                    .Where(u => u.Email != null && emails.Contains(u.Email))
                    .Select(u => new { u.Email, u.PhoneNumber })
                    .ToListAsync(ct))
                .GroupBy(u => u.Email!)
                .ToDictionary(g => g.Key, g => g.First().PhoneNumber ?? string.Empty);

            return new Counts(groups, students, teachers, phones);
        }

        private static AdminSubscriberDto Map(AppTenantInfo t, Counts c)
        {
            bool isCenter = t.Type == TenantType.Center;
            return new AdminSubscriberDto
            {
                Id = t.Id,
                Name = t.Name,
                Type = t.Type.ToString(),
                OwnerName = $"{t.FirstName} {t.LastName}".Trim(),
                OwnerEmail = t.Email ?? string.Empty,
                OwnerPhone = !string.IsNullOrEmpty(t.Email) && c.Phones.TryGetValue(t.Email, out var p) ? p : string.Empty,
                IsActive = t.IsActive,
                ValidUpTo = t.ValidUpTo,
                GroupsCount = c.Groups.TryGetValue(t.Id, out var g) ? g : 0,
                StudentsCount = c.Students.TryGetValue(t.Id, out var s) ? s : 0,
                // A center's teachers come from its active member teachers; an individual
                // workspace is always exactly one teacher (the owner).
                TeachersCount = isCenter ? (c.Teachers.TryGetValue(t.Id, out var tc) ? tc : 0) : 1,
                MaxTeachers = isCenter ? t.MaxTeachers : null,
            };
        }
    }
}
