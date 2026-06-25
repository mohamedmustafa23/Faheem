namespace Application.Features.Attendance.DTOs
{
    public class CenterScanRequest
    {
        /// <summary>The signed check-in token scanned from a student's screen.</summary>
        public string Token { get; set; } = string.Empty;
    }
}
