namespace Application.Features.Attendance.DTOs
{
    public class ScanQrRequest
    {
        public Guid OccurrenceId { get; set; }
        public string QrToken { get; set; } = string.Empty;
    }
}
