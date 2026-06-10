namespace Application.Features.Identity.DTOs
{
    public class RegisterStudentRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string EducationalStage { get; set; } = string.Empty;
        public string GradeYear { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;

        // Optional: if the student was added by a teacher (ghost) and given a code,
        // providing it here claims that existing account instead of creating a new one.
        public string? StudentCode { get; set; }
    }
}
