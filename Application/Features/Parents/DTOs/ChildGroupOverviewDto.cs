namespace Application.Features.Parents.DTOs
{
    // One row per group the child is enrolled in — the group-centric snapshot the
    // parent sees: standing (rank), attendance, grades, and fees, all for THIS child
    // in THIS group. Rank is computed across the group's students from a blend of
    // grade average + attendance (excused absences are neutral, never counted).
    public class ChildGroupOverviewDto
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;

        // Attendance (this child, this group). Rate denominator excludes Excused.
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Excused { get; set; }
        public int TotalCompleted { get; set; }
        public double AttendanceRate { get; set; }

        // Grades (this child, this group).
        public double? GradesAveragePercent { get; set; }
        public int ExamsCount { get; set; }

        // Standing in the group. Rank is null when the child has neither grades nor
        // counted attendance yet; RankedStudents is how many students are ranked.
        public int? Rank { get; set; }
        public int RankedStudents { get; set; }

        // Fees (this child, this group).
        public decimal TotalExpected { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }

        // Payment history (this child, this group), newest first — so the parent
        // sees each instalment: how much, when, and against which cycle.
        public List<ChildPaymentEntryDto> Payments { get; set; } = new();
    }

    public class ChildPaymentEntryDto
    {
        public DateTime PaidAt { get; set; }
        public decimal Amount { get; set; }
        public string CycleLabel { get; set; } = string.Empty;
    }
}
