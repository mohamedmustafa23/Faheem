using Domain.Contracts;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a recurring schedule template (e.g. "Every Saturday 2pm–4pm").
    /// Actual class meetings are tracked in SessionOccurrence.
    /// </summary>
    public class Session : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        public string TenantId { get; set; } = string.Empty;

        public ICollection<SessionOccurrence> Occurrences { get; set; } = new List<SessionOccurrence>();
    }
}
