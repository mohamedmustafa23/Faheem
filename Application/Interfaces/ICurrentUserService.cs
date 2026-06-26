namespace Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? TenantId { get; }
        bool IsGlobalUser { get; }

        /// <summary>The user's role in the selected workspace: "Owner", "Teacher" or "Assistant" (null if none).</summary>
        string? WorkspaceRole { get; }

        /// <summary>
        /// True when the user is a plain member teacher inside a workspace (a center
        /// member). Such a user only sees their own groups, never the whole workspace.
        /// </summary>
        bool IsWorkspaceMemberTeacher { get; }
    }
}
