using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public UserType UserType { get; set; }
        public StudentProfile? StudentProfile { get; set; }
        public bool IsGhostAccount { get; set; } = false;

        // Unique claim code for manually-added (ghost) students. The teacher gives
        // it to the student/parent; it's the secure key to (a) add the same student
        // to another teacher's group, and (b) claim the account on first sign-up.
        // Null for normal accounts.
        public string? StudentCode { get; set; }
        public ICollection<ParentStudentLink> MyChildren { get; set; } = [];
        public ICollection<ParentStudentLink> MyParentRequests { get; set; } = [];
        public virtual ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();
    }
}
