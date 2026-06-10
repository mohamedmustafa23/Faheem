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

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}