using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Identity.Models
{
    public class UserRefreshToken
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string TokenHash { get; set; } = string.Empty;

        public string JwtId { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; }
        public DateTime ExpiresOn { get; set; }

        public bool IsRevoked { get; set; }

        /// <summary>
        /// The workspace (Finbuckle tenant identifier) this session was issued for.
        /// A user can belong to several workspaces, so the SELECTED one is a property
        /// of the session — not of the user — and lives here on the refresh token.
        /// Read on refresh to re-issue a JWT scoped to the same workspace, instead of
        /// re-parsing the fragile "tenant" claim off the expired access token.
        /// Null for tokens issued before this column existed (handled via fallback).
        /// </summary>
        public string? WorkspaceIdentifier { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}