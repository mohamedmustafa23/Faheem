namespace Application.Features.Tokens.DTOs
{
    public class SelectWorkspaceRequest
    {
        /// <summary>The workspace (tenant) the user is choosing to sign into.</summary>
        public string TenantId { get; set; } = string.Empty;
    }
}
