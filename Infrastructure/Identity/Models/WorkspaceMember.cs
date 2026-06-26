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

        /// <summary>
        /// Operational capabilities this member has inside the workspace (center model).
        /// Merged into the JWT's permission claims on top of the Identity-role permissions.
        /// Owner = All; an individual teacher's own membership leaves this None (their
        /// toolkit comes from the Teacher role). Ignored for Individual workspaces.
        /// </summary>
        public CenterPermissions Permissions { get; set; } = CenterPermissions.None;

        /// <summary>
        /// The center's cut of this teacher's revenue, as a percentage (e.g. 30 ⇒ center
        /// takes 30%, teacher keeps 70%). Set by the center owner; null for the owner's own
        /// membership and for individual workspaces.
        /// </summary>
        public decimal? SharePercent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
    }
}
