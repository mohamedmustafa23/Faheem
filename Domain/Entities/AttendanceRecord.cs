using Domain.Contracts;
using Domain.Enums;

namespace Domain.Entities
{
    public class AttendanceRecord : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid OccurrenceId { get; set; }
        public SessionOccurrence Occurrence { get; set; } = null!;

        public string StudentId { get; set; } = string.Empty;

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;

        public DateTime MarkedAt { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        public bool IsScannedViaQR { get; set; } = false;

        public string TenantId { get; set; } = string.Empty;
    }
}
