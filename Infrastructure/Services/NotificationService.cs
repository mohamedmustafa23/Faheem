using Application.Exceptions;
using Application.Features.Notifications.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Contexts;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly TenantDbContext _tenantDbContext;
        private readonly IPushNotificationService _pushService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext dbContext, TenantDbContext tenantDbContext, IPushNotificationService pushService, ILogger<NotificationService> logger)
        {
            _dbContext = dbContext;
            _tenantDbContext = tenantDbContext;
            _pushService = pushService;
            _logger = logger;
        }

        public async Task<string> SaveDeviceTokenAsync(SaveDeviceTokenRequest request, string userId, CancellationToken ct = default)
        {
            // First: detach this token from any *other* user. Two accounts logging
            // into the same physical device would otherwise both keep claiming
            // the token and double-deliver notifications to whoever's currently
            // signed in.
            var otherUsersClaiming = await _dbContext.UserDevices
                .Where(d => d.FcmToken == request.FcmToken && d.UserId != userId && d.IsActive)
                .ToListAsync(ct);
            foreach (var d in otherUsersClaiming)
                d.IsActive = false;

            var existingDevice = await _dbContext.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.FcmToken == request.FcmToken, ct);

            if (existingDevice != null)
            {
                existingDevice.LastUsedAt = DateTime.UtcNow;
                existingDevice.IsActive = true;
                existingDevice.DeviceName = request.DeviceName ?? existingDevice.DeviceName;
                existingDevice.Platform   = request.Platform   ?? existingDevice.Platform;
            }
            else
            {
                _dbContext.UserDevices.Add(new UserDevice
                {
                    UserId = userId,
                    FcmToken = request.FcmToken,
                    DeviceName = request.DeviceName,
                    Platform = request.Platform
                });
            }

            await _dbContext.SaveChangesAsync(ct);
            return "Device token saved successfully.";
        }

        public async Task<string> DeleteDeviceTokenAsync(string fcmToken, string userId, CancellationToken ct = default)
        {
            // Deactivate rather than delete: it preserves analytics (when was this
            // token last seen?) and keeps the row available for token rotation
            // detection if the same user logs back in later.
            var device = await _dbContext.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.FcmToken == fcmToken, ct);

            if (device != null)
            {
                device.IsActive = false;
                await _dbContext.SaveChangesAsync(ct);
            }
            return "Device token unregistered.";
        }

        public async Task<PaginatedResult<NotificationResponseDto>> GetMyNotificationsAsync(string userId, int page = 1, int pageSize = 20, bool markAsRead = true, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync(ct);

            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var tenantIds = notifications.Select(n => n.TenantId).Distinct().ToList();
            var tenants = await _tenantDbContext.TenantInfo
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

            if (markAsRead)
            {
                var unreadNotifications = notifications.Where(n => !n.IsRead).ToList();
                if (unreadNotifications.Any())
                {
                    unreadNotifications.ForEach(n => n.IsRead = true);
                    await _dbContext.SaveChangesAsync(ct);
                }
            }

            var data = notifications.Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                TeacherName = tenants.TryGetValue(n.TenantId, out var tName) ? tName : "System",
                Route = n.Route
            }).ToList();

            return PaginatedResult<NotificationResponseDto>.Create(data, totalCount, page, pageSize);
        }

        public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
        {
            // Pure read — no side effects, safe to poll from a badge.
            return await _dbContext.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
        }

        public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct = default)
        {
            // Bulk update — fast path even with hundreds of unread items because
            // EF Core 7+ compiles this to a single UPDATE statement.
            return await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(set => set.SetProperty(n => n.IsRead, true), ct);
        }

        public async Task SendSystemNotificationAsync(List<string> studentIds, string title, string message, NotificationType type, string tenantId, string? route = null, CancellationToken ct = default)
        {
            if (studentIds == null || !studentIds.Any()) return;

            string[] studentIdsArray = studentIds.ToArray();

            var parentIds = await _dbContext.ParentStudentLinks
                .Where(l => studentIdsArray.Contains(l.StudentUserId) && l.Status == LinkStatus.Accepted)
                .Select(l => l.ParentUserId)
                .Distinct()
                .ToListAsync(ct);

            var allTargetUserIds = studentIds.Concat(parentIds).Distinct().ToList();

            await PersistAndPushAsync(allTargetUserIds, title, message, type, tenantId, route, ct);
        }

        public async Task SendStudentAndParentNotificationsAsync(
            List<string> studentIds,
            NotificationPayload studentPayload,
            Func<string, string, NotificationPayload?> parentPayloadFactory,
            string tenantId,
            CancellationToken ct = default)
        {
            if (studentIds == null || studentIds.Count == 0) return;

            var distinctStudentIds = studentIds.Distinct().ToList();

            // Look up display names once so parent messages can address the
            // right child ("غاب ابنك أحمد"). Falls back to "ابنك"/"ابنتك" when
            // we can't resolve a name — never let an empty name leak into a UI.
            var studentNames = await _dbContext.Users
                .Where(u => distinctStudentIds.Contains(u.Id))
                .Select(u => new { u.Id, FullName = (u.FirstName + " " + u.LastName).Trim() })
                .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);

            // 1) Persist + push the student-facing copy to every student.
            await PersistAndPushAsync(distinctStudentIds, studentPayload.Title, studentPayload.Message, studentPayload.Type, tenantId, studentPayload.Route, ct);

            // 2) Resolve every accepted parent link for these students. We keep
            //    the link rows (not just parent IDs) so we can build a
            //    per-child message for each parent — a parent of two kids in
            //    the same group should get two separate notifications.
            var parentLinks = await _dbContext.ParentStudentLinks
                .Where(l => distinctStudentIds.Contains(l.StudentUserId) && l.Status == LinkStatus.Accepted)
                .Select(l => new { l.ParentUserId, l.StudentUserId })
                .ToListAsync(ct);

            if (parentLinks.Count == 0) return;

            // Build per-parent payloads from the factory and dispatch one at a
            // time. We could batch within a route, but each parent's payload
            // is unique by child name, so persistence is per-row anyway. The
            // push call still fans-out to all of that parent's devices.
            foreach (var link in parentLinks)
            {
                if (!studentNames.TryGetValue(link.StudentUserId, out var name) || string.IsNullOrWhiteSpace(name))
                    name = "ابنك";

                var parentPayload = parentPayloadFactory(link.StudentUserId, name);
                if (parentPayload == null) continue;

                await PersistAndPushAsync(
                    new List<string> { link.ParentUserId },
                    parentPayload.Title,
                    parentPayload.Message,
                    parentPayload.Type,
                    tenantId,
                    parentPayload.Route,
                    ct);
            }
        }

        public async Task SendToUsersAsync(List<string> userIds, string title, string message, NotificationType type, string tenantId, string? route = null, CancellationToken ct = default)
        {
            if (userIds == null || userIds.Count == 0) return;
            var distinct = userIds.Distinct().ToList();
            await PersistAndPushAsync(distinct, title, message, type, tenantId, route, ct);
        }

        private async Task PersistAndPushAsync(List<string> userIds, string title, string message, NotificationType type, string tenantId, string? route, CancellationToken ct)
        {
            if (userIds.Count == 0) return;

            var notificationsToSave = userIds.Select(userId => new Notification
            {
                UserId   = userId,
                Title    = title,
                Message  = message,
                Type     = type,
                Route    = route,
                TenantId = tenantId
            }).ToList();

            await _dbContext.Notifications.AddRangeAsync(notificationsToSave, ct);
            await _dbContext.SaveChangesAsync(ct);

            var fcmTokens = await _dbContext.UserDevices
                .Where(d => userIds.Contains(d.UserId) && d.IsActive && !string.IsNullOrEmpty(d.FcmToken))
                .Select(d => d.FcmToken!)
                .Distinct()
                .ToListAsync(ct);

            if (fcmTokens.Any())
            {
                // FCM data values must be strings. We only attach `route` when
                // present — empty values would still show as keys on the client.
                IReadOnlyDictionary<string, string>? data = !string.IsNullOrWhiteSpace(route)
                    ? new Dictionary<string, string> { ["route"] = route }
                    : null;

                await _pushService.SendAsync(fcmTokens, title, message, data, ct);
            }
        }
    }
}