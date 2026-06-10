namespace Application.Features.Attendance.DTOs
{
    public class StudentAttendanceSummaryDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int TotalCompleted { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Excused { get; set; }
        public double AttendanceRate { get; set; }
    }
}
