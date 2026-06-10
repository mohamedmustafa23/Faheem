using Domain.Enums;

namespace Application.Features.Notifications.DTOs
{
    /// <summary>
    /// A single notification payload — title, body, deep-link route, and type.
    /// Lets call sites build per-recipient messages (e.g. a parent sees a
    /// different message than the student, with a different deep link).
    /// </summary>
    public sealed class NotificationPayload
    {
        public string Title   { get; }
        public string Message { get; }
        public NotificationType Type { get; }

        /// <summary>
        /// Optional deep-link target. When set, becomes the notification's
        /// <c>Route</c> and is forwarded as FCM <c>data.route</c> for tap routing.
        /// </summary>
        public string? Route { get; }

        public NotificationPayload(string title, string message, NotificationType type, string? route = null)
        {
            Title   = title;
            Message = message;
            Type    = type;
            Route   = route;
        }
    }
}
