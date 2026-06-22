using Domain.Enums;

namespace Infrastructure.Identity.Models
{
    /// <summary>
    /// The source of truth for "which user belongs to which workspace, and as what".
    /// Replaces the old single tenant claim per user: a user may now have many
    /// memberships (their private workspace + one or more centers). The JWT's
    /// tenant claim is derived from the membership the user selects at login.
    /// </summary>
    public class WorkspaceMember
    {
        public Guid Id { get; set; }

        /// <summary>FK → AspNetUsers (ApplicationUser.Id).</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>The workspace (tenant) this membership grants access to. Matches AppTenantInfo.Id.</summary>
        public string TenantId { get; set; } = string.Empty;

        public WorkspaceRole Role { get; set; }

        public WorkspaceMemberStatus Status { get; set; } = WorkspaceMemberStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
    }
}
