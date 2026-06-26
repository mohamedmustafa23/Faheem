namespace Application.Features.Identity.DTOs
{
    public class AssistantDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        /// <summary>The assistant's capability flags (CenterPermissions bitmask).</summary>
        public int Permissions { get; set; }
    }
}
