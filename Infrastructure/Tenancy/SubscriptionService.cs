using Application.Exceptions;
using Application.Features.Tenancy.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenancy
{
    // Centralised subscription lifecycle. The admin panel, a future payment webhook,
    // and self-serve renewal all go through ExtendAsync — activation logic in ONE place.
    public class SubscriptionService : ISubscriptionService
    {
        // When the owner gets nudged to renew. Daily job → each fires exactly once.
        private static readonly int[] ReminderThresholds = { 7, 3, 1, 0 };

        private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
        private readonly ApplicationDbContext _appDb;
        private readonly INotificationService _notifications;

        public SubscriptionService(
            IMultiTenantStore<AppTenantInfo> tenantStore,
            ApplicationDbContext appDb,
            INotificationService notifications)
        {
            _tenantStore = tenantStore;
            _appDb = appDb;
            _notifications = notifications;
        }

        public async Task<SubscriptionStatusDto?> GetStatusAsync(string tenantId, CancellationToken ct = default)
        {
            var t = await _tenantStore.TryGetAsync(tenantId);
            return t is null ? null : Status(t);
        }

        public async Task ExtendAsync(string tenantId, int months, int? maxTeachers = null, CancellationToken ct = default)
        {
            if (maxTeachers is < 1)
                throw new ConflictException(["عدد المقاعد لازم يكون 1 على الأقل."]);

            var t = await _tenantStore.TryGetAsync(tenantId)
                ?? throw new NotFoundException(["المشترك مش موجود."]);

            // Renew from the later of (now, current expiry) so early renewal keeps unused days.
            var from = t.ValidUpTo > DateTime.UtcNow ? t.ValidUpTo : DateTime.UtcNow;
            t.ValidUpTo = from.AddMonths(months);
            t.IsActive = true;

            // A center is priced on seats × duration — set the seat limit in the same step
            // so "renew" is one coherent action, not two. Ignored for individual workspaces.
            if (maxTeachers.HasValue && t.Type == TenantType.Center)
                t.MaxTeachers = maxTeachers.Value;

            await _tenantStore.TryUpdateAsync(t);
        }

        public async Task SetActiveAsync(string tenantId, bool isActive, CancellationToken ct = default)
        {
            var t = await _tenantStore.TryGetAsync(tenantId)
                ?? throw new NotFoundException(["المشترك مش موجود."]);
            t.IsActive = isActive;
            await _tenantStore.TryUpdateAsync(t);
        }

        public async Task<int> SendDueRemindersAsync(CancellationToken ct = default)
        {
            var candidates = (await _tenantStore.GetAllAsync())
                .Where(t => t.Id != TenancyConstants.Root.Id && t.IsActive)
                .Select(t => new { Tenant = t, DaysLeft = DaysLeft(t.ValidUpTo) })
                .Where(x => ReminderThresholds.Contains(x.DaysLeft))
                .ToList();
            if (candidates.Count == 0) return 0;

            // Owner = the user whose account email matches the workspace email.
            var emails = candidates.Select(x => x.Tenant.Email).Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
            var ownerByEmail = (await _appDb.Users.IgnoreQueryFilters()
                    .Where(u => u.Email != null && emails.Contains(u.Email))
                    .Select(u => new { u.Email, u.Id })
                    .ToListAsync(ct))
                .GroupBy(u => u.Email!)
                .ToDictionary(g => g.Key, g => g.First().Id);

            // Dedup: never remind an owner twice in the same day — guards against the
            // job running more than once (app restart / sleepy free hosting).
            var todayStart = DateTime.UtcNow.Date;
            var ownerIds = ownerByEmail.Values.Distinct().ToList();
            var remindedToday = (await _appDb.Notifications.IgnoreQueryFilters()
                    .Where(n => n.Type == NotificationType.SubscriptionExpiring
                                && ownerIds.Contains(n.UserId)
                                && n.CreatedAt >= todayStart)
                    .Select(n => n.UserId)
                    .Distinct()
                    .ToListAsync(ct))
                .ToHashSet();

            int notified = 0;
            foreach (var x in candidates)
            {
                if (string.IsNullOrEmpty(x.Tenant.Email)
                    || !ownerByEmail.TryGetValue(x.Tenant.Email, out var ownerId)
                    || remindedToday.Contains(ownerId))
                    continue;

                string when = x.DaysLeft <= 0 ? "النهاردة" : $"خلال {x.DaysLeft} يوم";
                string route = x.Tenant.Type == TenantType.Center ? "/center" : "/teacher";

                await _notifications.SendToUsersAsync(
                    new List<string> { ownerId },
                    "اشتراكك قرب يخلص",
                    $"اشتراكك في جوكو بيخلص {when}. جدّد عشان تفضل شغّال من غير توقف.",
                    NotificationType.SubscriptionExpiring,
                    x.Tenant.Id,
                    route,
                    ct);
                remindedToday.Add(ownerId); // also guards against duplicates within this run
                notified++;
            }
            return notified;
        }

        private static int DaysLeft(DateTime validUpTo)
            => (int)Math.Ceiling((validUpTo - DateTime.UtcNow).TotalDays);

        private static SubscriptionStatusDto Status(AppTenantInfo t)
        {
            int daysLeft = DaysLeft(t.ValidUpTo);
            string status = !t.IsActive ? "suspended"
                : daysLeft < 0 ? "expired"
                : daysLeft <= 7 ? "expiring"
                : "active";

            return new SubscriptionStatusDto
            {
                Name = t.Name,
                Type = t.Type.ToString(),
                IsActive = t.IsActive,
                ValidUntil = t.ValidUpTo,
                DaysLeft = daysLeft,
                Status = status,
            };
        }
    }
}
