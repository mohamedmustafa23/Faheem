namespace Application.Features.Parents.DTOs
{
    /// <summary>
    /// Lightweight "at-a-glance" snapshot used by both the parent's dashboard
    /// card per child AND the Overview tab inside child details. Designed for
    /// a single round-trip so the dashboard loads instantly.
    /// </summary>
    public class ChildOverviewDto
    {
        public LinkedChildDto Child { get; set; } = new();

        // ── Attendance (across all enrolled groups) ──
        public int AttendanceRate { get; set; }      // 0-100
        public int AttendanceStreak { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Excused { get; set; }
        public int TotalCompletedSessions { get; set; }

        // ── Grades (across all groups) ──
        public int? GradesAveragePercent { get; set; } // null when no grades yet
        public int ExamsCount { get; set; }

        // ── Payments (across all groups) ──
        public decimal TotalRemaining { get; set; }
        public decimal TotalPaid { get; set; }
        public int GroupsWithOutstanding { get; set; }

        // ── Today ──
        public int TodaySessionsCount { get; set; }
        public ChildNextSessionDto? NextSession { get; set; }
    }

    public class ChildNextSessionDto
    {
        public Guid? OccurrenceId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string StartTime { get; set; } = string.Empty; // "HH:mm:ss"
        public string EndTime { get; set; } = string.Empty;
        /// <summary>Negative once the session has started — caller can render "جارية الآن".</summary>
        public int MinutesUntilStart { get; set; }
    }

    /// <summary>
    /// A single announcement enriched with the group it came from — needed by
    /// the parent's announcements tab where messages from multiple groups are
    /// interleaved chronologically.
    /// </summary>
    public class ChildAnnouncementDto
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? TeacherName { get; set; }
    }
}
