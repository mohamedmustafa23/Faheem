namespace Application.Features.Grades.DTOs
{
    public class SaveGradesRequest
    {
        public Guid ExamId { get; set; }
        public List<StudentScoreInput> StudentScores { get; set; } = new();
    }

    public class StudentScoreInput
    {
        public string StudentId { get; set; } = string.Empty;
        public decimal Score { get; set; }
    }
}