namespace Application.Features.Payments.DTOs
{
    /// <summary>
    /// One-shot grand-total finance picture for the teacher across EVERY group
    /// and EVERY cycle they own (including closed cycles + standalone sessions).
    /// Powers the headline finance card so the teacher can answer at a glance:
    ///   • كام جمعت من أول السنة؟
    ///   • كام لسه فاضل عليّا؟
    ///   • أي مجموعة فيها فلوس متأخرة؟
    /// Waived records contribute 0 to "expected" — they're forgiven.
    /// </summary>
    public class TeacherFinancialOverviewDto
    {
        // ── Headline totals (across all groups, all cycles, all standalones) ──
        public decimal TotalCollected { get; set; }
        public decimal TotalExpected  { get; set; }
        public decimal TotalRemaining => TotalExpected - TotalCollected;

        // ── Counts that frame the totals ──────────────────────────────────────
        public int GroupsCount              { get; set; }
        public int ActiveStudentsCount      { get; set; } // unique students enrolled across the teacher's groups
        public int OutstandingStudentsCount { get; set; } // students with remaining > 0 on any record
        public int CyclesCount              { get; set; } // every cycle ever opened (closed + open)
        public int OpenCyclesCount          { get; set; }

        // ── Per-group breakdown, ordered by Remaining DESC so the worst
        //    debtors are at the top of the parent's eye-line. ───────────────
        public List<TeacherFinancialGroupRow> Groups { get; set; } = new();
    }

    public class TeacherFinancialGroupRow
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? Subject  { get; set; }

        public decimal Collected { get; set; }
        public decimal Expected  { get; set; }
        public decimal Remaining => Expected - Collected;

        public int StudentsCount { get; set; }
        public int CyclesCount   { get; set; }
        public int OpenCyclesCount { get; set; }
    }
}
