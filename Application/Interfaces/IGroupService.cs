using Application.Features.Groups.DTOs;

namespace Application.Interfaces
{
    public interface IGroupService
    {
        Task<string> CreateGroupAsync(CreateGroupRequest request, string tenantId, CancellationToken ct = default);
        Task<List<GroupResponseDto>> GetTeacherGroupsAsync(string tenantId, CancellationToken ct = default);
        Task<string> UpdateGroupAsync(UpdateGroupRequest request, string tenantId, CancellationToken ct = default);
        Task<string> DeleteGroupAsync(Guid groupId, string tenantId, CancellationToken ct = default);
        Task<GroupDetailsResponseDto> GetGroupDetailsAsync(Guid groupId, string tenantId, CancellationToken ct = default);
        Task<string> RegenerateCodeAsync(Guid groupId, string tenantId, CancellationToken ct = default);
        Task<string> TogglePinAsync(Guid groupId, string tenantId, CancellationToken ct = default);
    }
}
