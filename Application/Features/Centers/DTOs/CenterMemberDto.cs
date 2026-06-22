namespace Application.Features.Centers.DTOs
{
    public class CenterMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// <summary>"Owner", "Teacher" or "Assistant".</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>"Active" or "Invited".</summary>
        public string Status { get; set; } = string.Empty;
    }
}
