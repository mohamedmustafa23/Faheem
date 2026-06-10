namespace Application.Features.Groups.DTOs
{
    public class GroupDetailsResponseDto
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
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? CurrentCycleSessionsCompleted { get; set; }

        public List<SessionDto> Schedules { get; set; } = new();
        public List<StudentDto> Students { get; set; } = new();
    }

}
