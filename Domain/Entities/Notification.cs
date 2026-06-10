using Domain.Contracts;
using Domain.Enums;

namespace Domain.Entities
{
    public class Notification : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional deep-link route (e.g. "/parent/children/abc-123" or
        /// "/student/groups/xyz"). When set, tapping the notification on
        /// the device or in the in-app list navigates here. Null = no deep
        /// link, the tap just opens the notifications screen.
        /// </summary>
        public string? Route { get; set; }

        public string TenantId { get; set; } = string.Empty;
    }
}