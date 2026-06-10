using Domain.Enums;

namespace Application.Features.Payments.DTOs
{
    /// <summary>
    /// One entry per group the student is enrolled in. Combines the open cycle
    /// (if any) with all standalone-occurrence balances tied to that group.
    /// Waived records are excluded from outstanding totals.
    /// </summary>
    public class StudentGroupPaymentsDto
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? TeacherName { get; set; }

        // ── Open cycle (null if the student has no open-cycle record yet) ──
        public StudentCycleRecordDto? CurrentCycle { get; set; }

        // ── Standalone (per-session) records ──
        public List<StudentStandaloneRecordDto> Standalone { get; set; } = [];

        // ── Group-level totals ──
        public decimal TotalExpected { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }

        /// <summary>True when the student currently owes money in this group.</summary>
        public bool HasOutstanding => TotalRemaining > 0;
    }

    public class StudentCycleRecordDto
    {
        public Guid RecordId { get; set; }
        public Guid CycleId { get; set; }
        public int CycleNumber { get; set; }
        public int SessionsCompleted { get; set; }
        public int SessionsTarget { get; set; }
        public bool IsCycleClosed { get; set; }

        public decimal ExpectedAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? DiscountReason { get; set; }
        public decimal NetExpected => ExpectedAmount - DiscountAmount;
        public decimal TotalPaid { get; set; }
        public decimal Remaining => Math.Max(0, NetExpected - TotalPaid);

        public PaymentStatus Status { get; set; }
        public List<PaymentTransactionDto> Transactions { get; set; } = [];
    }

    public class StudentStandaloneRecordDto
    {
        public Guid RecordId { get; set; }
        public Guid OccurrenceId { get; set; }
        public DateOnly OccurrenceDate { get; set; }

        public decimal ExpectedAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? DiscountReason { get; set; }
        public decimal NetExpected => ExpectedAmount - DiscountAmount;
        public decimal TotalPaid { get; set; }
        public decimal Remaining => Math.Max(0, NetExpected - TotalPaid);

        public PaymentStatus Status { get; set; }
        public List<PaymentTransactionDto> Transactions { get; set; } = [];
    }

    /// <summary>Top-level "my finance" view for a student: totals + per-group breakdown.</summary>
    public class StudentPaymentsOverviewDto
    {
        public decimal TotalExpected { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
        public int GroupsWithOutstanding { get; set; }

        public List<StudentGroupPaymentsDto> Groups { get; set; } = [];
    }
}
