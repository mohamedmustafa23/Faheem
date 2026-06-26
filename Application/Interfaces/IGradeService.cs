using Application.Features.Grades.DTOs;

namespace Application.Interfaces
{
    public interface IGradeService
    {
        Task<Guid> CreateExamAsync(CreateExamRequest request, string tenantId, CancellationToken ct = default);
        Task<string> DeleteExamAsync(Guid examId, string tenantId, CancellationToken ct = default);
        Task<string> SaveGradesAsync(SaveGradesRequest request, string tenantId, CancellationToken ct = default);
        Task<List<StudentGradeResponseDto>> GetStudentGradesAsync(string studentId, CancellationToken ct = default);
        Task<List<GroupExamResponseDto>> GetGroupExamsForTeacherAsync(Guid groupId, string tenantId, CancellationToken ct = default);
        Task<List<ExamScoreResponseDto>> GetExamGradesForTeacherAsync(Guid examId, string tenantId, CancellationToken ct = default);
    }
}