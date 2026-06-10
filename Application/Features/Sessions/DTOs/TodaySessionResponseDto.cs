namespace Application.Features.Sessions.DTOs
{
    public class TodaySessionResponseDto
    {
        public Guid OccurrenceId { get; set; }
        public Guid? ScheduleId { get; set; }  // null for manual (non-recurring) sessions
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateOnly OccurrenceDate { get; set; }
    }
}
