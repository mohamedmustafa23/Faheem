using Domain.Enums;

namespace Application.Features.LessonReports.DTOs
{
    // Teacher-side: everything the report screen needs in one round-trip — the
    // (optional) saved summary plus a row per PRESENT student, pre-filled from any
    // existing report so re-opening shows what was already entered.
    public class LessonReportEditorDto
    {
        public Guid OccurrenceId { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public DateOnly Date { get; set; }

        public bool HasReport { get; set; }
        public string? LessonTopic { get; set; }
        public string? Homework { get; set; }

        public List<StudentFeedbackDto> Students { get; set; } = new();
    }

    public class StudentFeedbackDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public PerformanceRating? Performance { get; set; }
        public ParticipationRating? Participation { get; set; }
        public HomeworkStatus? HomeworkResult { get; set; }
        public string? Note { get; set; }
    }
}
