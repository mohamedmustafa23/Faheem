namespace Application.Features.Sessions.DTOs
{
    public class UpdateSessionRequest
    {
        public Guid ScheduleId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
