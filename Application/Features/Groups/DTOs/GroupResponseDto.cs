namespace Application.Features.Groups.DTOs
{
    public class GroupResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string EducationalStage { get; set; } = string.Empty;
        public string GradeYear { get; set; } = string.Empty;
        public string EnrollmentCode { get; set; } = string.Empty;
        public int? MaxStudents { get; set; }
        public int? SessionsPerCycle { get; set; }
        public decimal? MonthlyFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public int EnrolledStudentsCount { get; set; }
        public int? CurrentCycleSessionsCompleted { get; set; }
        public bool IsPinned { get; set; }
    }
}
