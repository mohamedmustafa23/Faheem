namespace Application.Features.Centers.DTOs
{
    /// <summary>One teacher's financial detail inside a center: totals + a per-group breakdown.
    /// Powers the owner's "enter a teacher" screen and the account statement.</summary>
    public class CenterTeacherDetailDto
    {
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public decimal? SharePercent { get; set; }

        public decimal Collected { get; set; }
        public decimal Expected { get; set; }
        public decimal Remaining { get; set; }
        public decimal CenterCut { get; set; }
        public decimal TeacherCut { get; set; }

        public List<CenterTeacherGroupRow> Groups { get; set; } = new();
    }

    public class CenterTeacherGroupRow
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        /// <summary>Fee per payment cycle per student (for the statement).</summary>
        public decimal? MonthlyFee { get; set; }
        public decimal Collected { get; set; }
        public decimal Expected { get; set; }
        public decimal Remaining { get; set; }
        public int StudentsCount { get; set; }
        public int OutstandingStudentsCount { get; set; }
    }
}
