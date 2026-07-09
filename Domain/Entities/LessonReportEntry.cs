using Domain.Contracts;
using Domain.Enums;

namespace Domain.Entities
{
    // One per student inside a LessonReport. Every field is optional so the teacher
    // can tap only what's relevant (or use "apply to all" and tweak the exceptions).
    // Mirrors StudentGrade.
    public class LessonReportEntry : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid LessonReportId { get; set; }
        public LessonReport LessonReport { get; set; } = null!;

        public string StudentId { get; set; } = string.Empty;

        public PerformanceRating? Performance { get; set; }
        public ParticipationRating? Participation { get; set; }
        public HomeworkStatus? HomeworkResult { get; set; }
        public string? Note { get; set; }

        public string TenantId { get; set; } = string.Empty;
    }
}
