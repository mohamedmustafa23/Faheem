using Domain.Enums;

namespace Application.Features.LessonReports.DTOs
{
    // Upsert payload for one session's lesson report. Everything except the
    // occurrence is optional — the teacher may skip the summary, the homework,
    // or any individual student's feedback.
    public class SaveLessonReportRequest
    {
        public Guid OccurrenceId { get; set; }
        public string? LessonTopic { get; set; }
        public string? Homework { get; set; }
        public List<StudentFeedbackInput> Entries { get; set; } = new();
    }

    public class StudentFeedbackInput
    {
        public string StudentId { get; set; } = string.Empty;
        public PerformanceRating? Performance { get; set; }
        public ParticipationRating? Participation { get; set; }
        public HomeworkStatus? HomeworkResult { get; set; }
        public string? Note { get; set; }
    }
}
