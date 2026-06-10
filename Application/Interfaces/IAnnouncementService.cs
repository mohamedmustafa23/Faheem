using Application.Features.Announcements.DTOs;

namespace Application.Interfaces
{
    public interface IAnnouncementService
    {
        Task<string> CreateAnnouncementAsync(CreateAnnouncementRequest request, string tenantId, CancellationToken ct = default);
        Task<string> DeleteAnnouncementAsync(Guid announcementId, string tenantId, CancellationToken ct = default);
        Task<string> TogglePinAsync(Guid announcementId, string tenantId, CancellationToken ct = default);
        Task<List<AnnouncementResponseDto>> GetGroupAnnouncementsAsync(Guid groupId, string userId, string userRole, CancellationToken ct = default);
    }
}