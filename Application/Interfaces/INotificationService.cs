using Application.Features.Notifications.DTOs;
using Application.Wrappers;
using Domain.Enums;

namespace Application.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Lists the user's notifications. When markAsRead=true (default), any unread
        /// items returned in this page are flipped to read in the same call. Pass
        /// markAsRead=false for read-only previews (e.g. dashboard recent-activity).
        /// </summary>
        Task<PaginatedResult<NotificationResponseDto>> GetMyNotificationsAsync(string userId, int page = 1, int pageSize = 20, bool markAsRead = true, CancellationToken ct = default);
        Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);

        /// <summary>Flips every unread notification for the user to read in one shot.</summary>
        Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct = default);
        Task<string> SaveDeviceTokenAsync(SaveDeviceTokenRequest request, string userId, CancellationToken ct = default);

        /// <summary>
        /// Deactivates the given FCM token for the current user — called on logout
        /// so the device stops receiving notifications meant for the prior user.
        /// </summary>
        Task<string> DeleteDeviceTokenAsync(string fcmToken, string userId, CancellationToken ct = default);

        /// <summary>
        /// Notifies the given student IDs AND any linked, accepted parents — all
        /// recipients get the same title/body. Use this only when the message
        /// makes sense word-for-word for both audiences (rare). Prefer the
        /// per-recipient overload below for parent-personalized messages.
        /// </summary>
        Task SendSystemNotificationAsync(List<string> studentIds, string title, string message, NotificationType type, string tenantId, string? route = null, CancellationToken ct = default);

        /// <summary>
        /// Sends a student-side payload to every student in <paramref name="studentIds"/>
        /// and, for each student, calls <paramref name="parentPayloadFactory"/> with
        /// that student's id + display name to build a parent-personalized payload
        /// (e.g. message includes the child's name, route deep-links to that child's
        /// detail screen). Returning null from the factory skips parent notification
        /// for that child.
        /// </summary>
        Task SendStudentAndParentNotificationsAsync(
            List<string> studentIds,
            NotificationPayload studentPayload,
            Func<string /*studentId*/, string /*studentName*/, NotificationPayload?> parentPayloadFactory,
            string tenantId,
            CancellationToken ct = default);

        /// <summary>
        /// Notifies the given user IDs directly — no parent fan-out, no transformations.
        /// Use this for teacher-facing or arbitrary-user events.
        /// </summary>
        Task SendToUsersAsync(List<string> userIds, string title, string message, NotificationType type, string tenantId, string? route = null, CancellationToken ct = default);
    }
}
