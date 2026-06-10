using Domain.Contracts;

namespace Domain.Entities
{
    public class GroupAnnouncement : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public string Message { get; set; } = string.Empty;
        public bool IsPinned { get; set; } = false; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string TenantId { get; set; } = string.Empty;
    }
}