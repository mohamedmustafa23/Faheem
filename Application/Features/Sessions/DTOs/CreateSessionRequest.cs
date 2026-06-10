namespace Application.Features.Sessions.DTOs
{
    public class CreateSessionRequest
    {
        public Guid GroupId { get; set; }
        public List<SessionTimeSlot> TimeSlots { get; set; } = new();
    }

    public class SessionTimeSlot
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
