using Application.Features.LessonReports.DTOs;

namespace Application.Interfaces
{
    public interface ILessonReportService
    {
        // Teacher: the editor payload for one occurrence (present students + any saved report).
        Task<LessonReportEditorDto> GetOccurrenceReportAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default);

        // Teacher: create or update the report for one occurrence (upsert).
        Task<string> SaveReportAsync(SaveLessonReportRequest request, string tenantId, CancellationToken ct = default);

        // Parent: the child's lesson reports in one group, newest first.
        Task<List<ChildLessonReportDto>> GetChildGroupReportsAsync(string childId, Guid groupId, CancellationToken ct = default);

        // Student: my own lesson reports in one group (guarded by enrollment).
        Task<List<ChildLessonReportDto>> GetStudentGroupReportsAsync(string studentId, Guid groupId, CancellationToken ct = default);
    }
}
