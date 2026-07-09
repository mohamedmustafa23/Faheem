using Domain.Enums;

namespace Application.Features.LessonReports.DTOs
{
    // Parent/student-side: one lesson's report for a specific child — the group
    // summary plus this child's personal feedback (null fields when the teacher
    // didn't fill them, e.g. the child was absent that day).
    public class ChildLessonReportDto
    {
        public Guid ReportId { get; set; }
        public DateOnly Date { get; set; }
        public string? LessonTopic { get; set; }
        public string? Homework { get; set; }

        public PerformanceRating? Performance { get; set; }
        public ParticipationRating? Participation { get; set; }
        public HomeworkStatus? HomeworkResult { get; set; }
        public string? Note { get; set; }
    }
}
