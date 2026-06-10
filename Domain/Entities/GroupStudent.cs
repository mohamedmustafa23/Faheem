using Domain.Contracts;

namespace Domain.Entities
{
    public class GroupStudent : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public string StudentId { get; set; } = string.Empty;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public string TenantId { get; set; } = string.Empty;
    }
}