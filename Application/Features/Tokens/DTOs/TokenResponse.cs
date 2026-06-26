namespace Application.Features.Tokens.DTOs
{
    public class TokenResponse
    {
        /// <summary>
        /// The bearer token. Normally a full access token. When
        /// <see cref="RequiresWorkspaceSelection"/> is true this is a short-lived
        /// "account token" that only authorises a call to /select-workspace.
        /// </summary>
        public string JwtToken { get; set; } = string.Empty;

        /// <summary>Empty while a workspace selection is still pending.</summary>
        public string RefreshToken { get; set; } = string.Empty;

        public DateTime RefreshTokenExpiryDate { get; set; }

        /// <summary>
        /// True when the user belongs to more than one workspace and must pick one
        /// before a full session is issued. The client should show a workspace
        /// picker populated from <see cref="Workspaces"/> and then call
        /// /select-workspace with the chosen tenant.
        /// </summary>
        public bool RequiresWorkspaceSelection { get; set; }

        /// <summary>Populated only when <see cref="RequiresWorkspaceSelection"/> is true.</summary>
        public List<WorkspaceOption> Workspaces { get; set; } = new();
    }

    public class WorkspaceOption
    {
        public string TenantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>"Individual" or "Center".</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>The user's role in this workspace: "Owner", "Teacher" or "Assistant".</summary>
        public string Role { get; set; } = string.Empty;
    }
}
