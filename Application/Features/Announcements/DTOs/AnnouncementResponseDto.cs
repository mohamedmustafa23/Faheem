namespace Application.Features.Announcements.DTOs
{
    public class AnnouncementResponseDto
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}