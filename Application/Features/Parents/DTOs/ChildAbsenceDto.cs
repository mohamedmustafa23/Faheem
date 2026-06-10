namespace Application.Features.Parents.DTOs
{
    /// <summary>
    /// One absence (or excused absence) entry for the parent's attendance tab
    /// timeline — answers "which session, when, was it excused, and why".
    /// </summary>
    public class ChildAbsenceDto
    {
        public Guid OccurrenceId { get; set; }

        /// <summary>Date of the missed session, as DateOnly so the client never
        /// has to fight a timezone shift to render it.</summary>
        public DateOnly OccurrenceDate { get; set; }

        /// <summary>"HH:mm:ss" format — same shape the rest of the parent API uses.</summary>
        public string StartTime { get; set; } = string.Empty;
        public string EndTime   { get; set; } = string.Empty;

        public Guid   GroupId   { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? Subject  { get; set; }

        /// <summary>"Absent" or "Excused" — the only two statuses surfaced here.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Whatever the teacher typed in the attendance Notes field
        /// (e.g. excuse reason). Null when no note was attached.</summary>
        public string? Notes { get; set; }
    }
}
