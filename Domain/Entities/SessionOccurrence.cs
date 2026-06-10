using Domain.Contracts;
using Domain.Enums;

namespace Domain.Entities
{
    public class SessionOccurrence : IMustHaveTenant
    {
        public Guid Id { get; set; }

        // Null for standalone (manual) occurrences not tied to a recurring schedule.
        public Guid? SessionId { get; set; }
        public Session? Session { get; set; }

        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public DateOnly OccurrenceDate { get; set; }

        // Used only for standalone occurrences; recurring ones inherit from Session.
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        // False = bonus/gift session, does not increment the payment cycle.
        public bool CountsForPayment { get; set; } = true;

        /// <summary>Payment behaviour for manual (standalone) sessions only.</summary>
        public SessionPaymentMode PaymentMode { get; set; } = SessionPaymentMode.Free;

        /// <summary>Price for Standalone or AddToCycle manual sessions. Null for recurring sessions.</summary>
        public decimal? SessionPrice { get; set; }

        /// <summary>
        /// For AddToCycle manual sessions: the payment cycle this session was attached to
        /// (used for SessionsCompleted accounting and revert-on-cancel/delete).
        /// Null for recurring / standalone / free occurrences.
        /// </summary>
        public Guid? PaymentCycleId { get; set; }
        public PaymentCycle? PaymentCycle { get; set; }

        public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

        public string? QrToken { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}
