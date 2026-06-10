namespace Application.Features.Notifications.DTOs
{
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TeacherName { get; set; } = string.Empty;

        /// <summary>
        /// Optional deep-link route. The mobile app uses this when the user
        /// taps the notification in the in-app list. Null = no deep link.
        /// </summary>
        public string? Route { get; set; }
    }
}