using Domain.Contracts;

namespace Domain.Entities
{
    /// <summary>
    /// One payment cycle for a group (e.g. sessions 1-8 = cycle 1).
    /// A new cycle opens automatically when the previous one completes.
    /// </summary>
    public class PaymentCycle : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public int CycleNumber { get; set; }

        public int SessionsTarget { get; set; }
        public int SessionsCompleted { get; set; } = 0;

        /// <summary>
        /// Snapshot of Group.MonthlyFee at the moment this cycle opened.
        /// Preserved even if the group fee changes later.
        /// </summary>
        public decimal BaseFee { get; set; } = 0;

        /// <summary>
        /// Cumulative extra amount added by AddToCycle manual sessions.
        /// Total expected per student = BaseFee + ExtraFee.
        /// </summary>
        public decimal ExtraFee { get; set; } = 0;

        public bool IsCompleted { get; set; } = false;

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public string TenantId { get; set; } = string.Empty;

        // Optimistic concurrency token — prevents two end-of-session handlers
        // from double-incrementing SessionsCompleted or double-closing the cycle.
        public byte[] RowVersion { get; set; } = [];

        public ICollection<StudentPaymentRecord> StudentRecords { get; set; } = new List<StudentPaymentRecord>();
    }
}
