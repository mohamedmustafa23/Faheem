namespace Application.Features.Centers.DTOs
{
    public class CenterMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// <summary>"Owner", "Teacher", "Assistant" or "Staff".</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>"Active" or "Invited".</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>The member's capability flags (CenterPermissions bitmask) — drives the permission editor.</summary>
        public int Permissions { get; set; }
    }
}
