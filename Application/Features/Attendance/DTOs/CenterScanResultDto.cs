namespace Application.Features.Attendance.DTOs
{
    /// <summary>What the center scanner shows after a successful scan.</summary>
    public class CenterScanResultDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public bool AlreadyPresent { get; set; }
    }
}
