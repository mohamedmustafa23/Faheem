namespace Application.Features.Grades.DTOs
{
    public class CreateExamRequest
    {
        public Guid GroupId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public decimal MaxScore { get; set; }
    }
}