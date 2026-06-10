using Application.Features.Materials.DTOs;

namespace Application.Interfaces
{
    public interface IMaterialService
    {
        Task<string> UploadMaterialAsync(UploadMaterialRequest request, string tenantId, CancellationToken ct = default);
        Task<List<MaterialResponseDto>> GetGroupMaterialsAsync(Guid groupId, string userId, CancellationToken ct = default);
        Task<List<MaterialResponseDto>> GetStudentAllMaterialsAsync(string studentId, CancellationToken ct = default);
        Task<List<MaterialResponseDto>> GetTeacherMaterialsAsync(Guid groupId, string tenantId, CancellationToken ct = default);
        Task<string> DeleteMaterialAsync(Guid materialId, string tenantId, CancellationToken ct = default);
    }
}