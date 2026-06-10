namespace Application.Features.Grades.DTOs
{
    public class StudentGradeResponseDto
    {
        public Guid ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public string TeacherName { get; set; } = string.Empty;

        // Group identifiers — needed by the student app to filter grades reliably
        // per-group (matching by group name is fragile when names repeat).
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
    }
}