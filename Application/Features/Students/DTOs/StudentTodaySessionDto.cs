namespace Application.Features.Students.DTOs
{
    public class StudentTodaySessionDto
    {
        public Guid OccurrenceId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
