namespace Application.Features.Students.DTOs
{
    /// <summary>
    /// Snapshot of an accepted parent–student link as the student sees it.
    /// Used by the "أهلي" panel in the student app to manage active parents.
    /// </summary>
    public class LinkedParentDto
    {
        public string ParentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public System.DateTime? LinkedSince { get; set; }
    }
}
