namespace Application.Features.Grades.DTOs
{
    public class ExamScoreResponseDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public decimal? Score { get; set; } 
    }
}