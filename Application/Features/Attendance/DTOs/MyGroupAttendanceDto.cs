namespace Application.Features.Attendance.DTOs
{
    public class MyGroupAttendanceDto
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int TotalCompleted { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Excused { get; set; }
        public double AttendanceRate { get; set; }
        public List<AttendanceEntryDto> History { get; set; } = [];
    }

    public class AttendanceEntryDto
    {
        public Guid OccurrenceId { get; set; }
        public DateOnly OccurrenceDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public string AttendanceStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool ScannedViaQR { get; set; }
    }
}
