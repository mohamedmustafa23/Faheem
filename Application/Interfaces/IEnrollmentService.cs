using Application.Features.Groups.DTOs;

namespace Application.Interfaces
{
    public interface IEnrollmentService
    {
        Task<string> JoinGroupAsync(string studentId, string enrollmentCode, CancellationToken ct = default);
        Task<string> RemoveStudentAsync(Guid groupId, string studentId, string tenantId, CancellationToken ct = default);
        Task<string> LeaveGroupAsync(Guid groupId, string studentId, CancellationToken ct = default);
        Task<string> ManualAddStudentAsync(ManualAddStudentRequest request, string tenantId, CancellationToken ct = default);
        Task<string> AddStudentByCodeAsync(Guid groupId, string studentCode, string tenantId, CancellationToken ct = default);
        Task<string> EditGhostStudentAsync(Guid groupId, string studentId, EditStudentRequest request, string tenantId, CancellationToken ct = default);
    }
}