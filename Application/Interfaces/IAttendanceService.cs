using Application.Features.Attendance.DTOs;
using Application.Wrappers;

namespace Application.Interfaces
{
    public interface IAttendanceService
    {
        Task<List<StudentAttendanceDto>> GetOccurrenceAttendanceAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default);
        Task<string> SaveAttendanceAsync(SaveAttendanceRequest request, string tenantId, CancellationToken ct = default);
        Task<string> EndOccurrenceAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default);
        Task<string> GenerateQrTokenAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default);
        Task<string> ScanQrCodeAsync(ScanQrRequest request, string studentId, CancellationToken ct = default);

        // History & reporting
        Task<List<StudentAttendanceSummaryDto>> GetGroupAttendanceSummaryAsync(Guid groupId, string tenantId, CancellationToken ct = default);
        Task<PaginatedResult<GroupOccurrenceDto>> GetGroupOccurrencesAsync(Guid groupId, int page, int pageSize, string tenantId, CancellationToken ct = default);
        Task<List<MyGroupAttendanceDto>> GetMyAttendanceSummaryAsync(string studentId, CancellationToken ct = default);
        Task<MyGroupAttendanceDto> GetMyGroupAttendanceDetailAsync(string studentId, Guid groupId, CancellationToken ct = default);

        /// <summary>
        /// Current attendance streak: the count of the student's most recent
        /// consecutive Present records across all groups, walking back from the
        /// newest. Stops at the first Absent or Excused. Used for the dashboard
        /// streak badge — purely cosmetic, no side effects.
        /// </summary>
        Task<int> GetMyAttendanceStreakAsync(string studentId, CancellationToken ct = default);
    }
}
