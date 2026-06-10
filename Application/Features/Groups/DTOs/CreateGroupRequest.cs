namespace Application.Features.Groups.DTOs
{
    public class CreateGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string EducationalStage { get; set; } = string.Empty;
        public string GradeYear { get; set; } = string.Empty;
        public int? MaxStudents { get; set; }
        public int? SessionsPerCycle { get; set; }
        /// <summary>Fee per payment cycle in local currency (e.g. 500 EGP).</summary>
        public decimal? MonthlyFee { get; set; }
        public string? Description { get; set; }
    }
}
