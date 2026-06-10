using Domain.Enums;

namespace Application.Features.Attendance.DTOs
{
    public class StudentAttendanceInput
    {
        public string StudentId { get; set; } = string.Empty;
        public AttendanceStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
