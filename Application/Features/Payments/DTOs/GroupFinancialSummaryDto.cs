namespace Application.Features.Payments.DTOs
{
    /// <summary>
    /// Aggregated financial picture for a group. Combines the current cycle's
    /// outstanding balances with all standalone-occurrence balances.
    /// Waived records are excluded from "expected" totals.
    /// </summary>
    public class GroupFinancialSummaryDto
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;

        /// <summary>Configured monthly fee on the group (null if not set).</summary>
        public decimal? MonthlyFee { get; set; }

        // ── Current cycle info ────────────────────────────────────────────────
        public int? CurrentCycleNumber { get; set; }
        public int SessionsCompleted { get; set; }
        public int SessionsTarget { get; set; }

        // ── Student counts (current cycle only) ───────────────────────────────
        public int TotalStudents { get; set; }
        public int PaidCount { get; set; }
        public int PartialCount { get; set; }
        public int UnpaidCount { get; set; }
        public int WaivedCount { get; set; }

        // ── Money (current cycle) ─────────────────────────────────────────────
        public decimal CycleCollected { get; set; }
        public decimal CycleExpected { get; set; }
        public decimal CyclePending => CycleExpected - CycleCollected;

        // ── Money (all standalone occurrences for this group) ─────────────────
        public decimal StandaloneCollected { get; set; }
        public decimal StandaloneExpected { get; set; }
        public decimal StandalonePending => StandaloneExpected - StandaloneCollected;

        // ── Grand totals ──────────────────────────────────────────────────────
        public decimal TotalCollected => CycleCollected + StandaloneCollected;
        public decimal TotalExpected  => CycleExpected  + StandaloneExpected;
        public decimal TotalPending   => TotalExpected  - TotalCollected;
    }
}
