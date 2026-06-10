using Domain.Enums;

namespace Infrastructure.Identity.Models
{
    public class ParentStudentLink
    {
        public Guid Id { get; set; }

        public string ParentUserId { get; set; } = string.Empty;
        public ApplicationUser Parent { get; set; } = null!;

        public string StudentUserId { get; set; } = string.Empty;
        public ApplicationUser Student { get; set; } = null!;

        public LinkStatus Status { get; set; } = LinkStatus.Pending;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedAt { get; set; }
    }

}