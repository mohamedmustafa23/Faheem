namespace Application.Features.Sessions.DTOs
{
    public class UpdateManualOccurrenceRequest
    {
        public DateOnly OccurrenceDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
