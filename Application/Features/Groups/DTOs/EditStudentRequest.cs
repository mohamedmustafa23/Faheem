namespace Application.Features.Groups.DTOs
{
    // Edit a manually-added (ghost) student. All fields optional — only provided
    // ones are applied. ParentPhoneNumber links a parent if given.
    public class EditStudentRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EducationalStage { get; set; }
        public string? GradeYear { get; set; }
        public string? ParentPhoneNumber { get; set; }
    }
}
