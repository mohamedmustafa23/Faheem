namespace Application.Features.Attendance.DTOs
{
    public class SaveAttendanceRequest
    {
        public Guid OccurrenceId { get; set; }
        public List<StudentAttendanceInput> Records { get; set; } = new();
    }
}
