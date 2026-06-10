namespace Infrastructure.Identity.Models
{
    public class StudentProfile
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public string EducationalStage { get; set; } = string.Empty;
        public string GradeYear {  get; set; } = string.Empty;

    }
}
