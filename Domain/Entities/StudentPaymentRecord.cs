using Domain.Contracts;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// One student's payment obligation for either a regular cycle
    /// or a standalone session occurrence.
    /// Invariant: exactly one of PaymentCycleId / OccurrenceId is set
    /// (enforced by a CHECK constraint at the DB layer).
    /// </summary>
    public class StudentPaymentRecord : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public string StudentId { get; set; } = string.Empty;

        public Guid GroupId { get; set; }

        public Guid? PaymentCycleId { get; set; }
        public PaymentCycle? PaymentCycle { get; set; }

        public Guid? OccurrenceId { get; set; }

        /// <summary>
        /// SessionsCompleted value at the moment this student joined the cycle.
        /// Used to understand mid-cycle joins.
        /// </summary>
        public int EnrolledAtSession { get; set; } = 0;

        /// <summary>
        /// The "list price" — the full amount this student would owe before any discount.
        /// May differ from BaseFee for mid-cycle joiners (e.g. AddToCycle propagation).
        /// </summary>
        public decimal ExpectedAmount { get; set; }

        /// <summary>
        /// Teacher-granted discount on this record (≥ 0, ≤ ExpectedAmount - TotalPaid).
        /// Net amount the student owes = ExpectedAmount - DiscountAmount.
        /// </summary>
        public decimal DiscountAmount { get; set; } = 0;

        /// <summary>Optional human note explaining why the discount was applied.</summary>
        public string? DiscountReason { get; set; }

        /// <summary>
        /// Unpaid / PartiallyPaid / Paid / Waived. Recomputed automatically whenever
        /// a PaymentTransaction is added or removed (or discount changes).
        /// </summary>
        public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

        public string TenantId { get; set; } = string.Empty;

        // Optimistic concurrency token — prevents two RecordPayment calls
        // from both observing a stale total and both flipping the status.
        public byte[] RowVersion { get; set; } = [];

        public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    }
}
