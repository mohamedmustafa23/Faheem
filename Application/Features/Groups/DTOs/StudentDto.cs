using System;

namespace Application.Features.Groups.DTOs
{
    public class StudentDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string EducationalStage { get; set; } = string.Empty;
        public string GradeYear { get; set; } = string.Empty;

        // Manually-added (ghost) students can be edited and re-added by code.
        public bool IsGhostAccount { get; set; }
        public string? StudentCode { get; set; }
        public bool IsLinkedToParent { get; set; }

        public DateTime JoinedAt { get; set; }
    }
}
