namespace Application.Features.Announcements.DTOs
{
    public class CreateAnnouncementRequest
    {
        public List<Guid> GroupIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool IsPinned { get; set; } = false;
    }
}