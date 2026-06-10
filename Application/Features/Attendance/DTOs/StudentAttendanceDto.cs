using Domain.Enums;

namespace Application.Features.Attendance.DTOs
{
    public class StudentAttendanceDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;
        public string? Notes { get; set; }
    }
}
