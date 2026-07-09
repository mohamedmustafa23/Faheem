using Domain.Contracts;

namespace Domain.Entities
{
    // One report per session occurrence. Holds the group-level summary (what was
    // covered + optional homework). Per-student feedback lives in LessonReportEntry.
    // Mirrors the Exam → StudentGrade shape.
    public class LessonReport : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid OccurrenceId { get; set; }
        public SessionOccurrence Occurrence { get; set; } = null!;

        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        // Both optional — a teacher may post per-student feedback with no group summary,
        // and not every session has homework.
        public string? LessonTopic { get; set; }
        public string? Homework { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string TenantId { get; set; } = string.Empty;

        public ICollection<LessonReportEntry> Entries { get; set; } = new List<LessonReportEntry>();
    }
}
