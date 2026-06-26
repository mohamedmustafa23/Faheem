namespace Application.Features.Centers.DTOs
{
    /// <summary>
    /// Center revenue report, computed on COLLECTED money (actual payments in).
    /// Outstanding (remaining) is surfaced for context, not split. Each teacher's
    /// row carries the center's cut (SharePercent) and the teacher's net share.
    /// </summary>
    public class CenterFinancialsDto
    {
        public decimal TotalCollected { get; set; }
        public decimal TotalExpected { get; set; }
        public decimal TotalRemaining { get; set; }

        /// <summary>Sum of the center's cut across all teachers.</summary>
        public decimal CenterShareTotal { get; set; }
        /// <summary>Sum of the teachers' net share across all teachers.</summary>
        public decimal TeachersShareTotal { get; set; }

        public int TeachersCount { get; set; }
        public List<CenterTeacherFinancialDto> Teachers { get; set; } = new();
    }

    public class CenterTeacherFinancialDto
    {
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;

        /// <summary>The center's cut of this teacher's revenue (0–100). Null = not set yet.</summary>
        public decimal? SharePercent { get; set; }

        public decimal Collected { get; set; }
        public decimal Expected { get; set; }
        public decimal Remaining { get; set; }

        public decimal CenterCut { get; set; }
        public decimal TeacherCut { get; set; }

        public int GroupsCount { get; set; }
        public int StudentsCount { get; set; }
        public int OutstandingStudentsCount { get; set; }
    }
}
