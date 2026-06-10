using Application.Features.Announcements.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Announcements.Queries
{
    public class GetGroupAnnouncementsQuery : IRequest<Result<List<AnnouncementResponseDto>>>
    {
        public Guid GroupId { get; set; }
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
        [JsonIgnore] public string UserRole { get; set; } = string.Empty;
    }

    public class GetGroupAnnouncementsQueryHandler : IRequestHandler<GetGroupAnnouncementsQuery, Result<List<AnnouncementResponseDto>>>
    {
        private readonly IAnnouncementService _announcementService;
        public GetGroupAnnouncementsQueryHandler(IAnnouncementService announcementService) => _announcementService = announcementService;

        public async Task<Result<List<AnnouncementResponseDto>>> Handle(GetGroupAnnouncementsQuery query, CancellationToken cancellationToken)
        {
            var result = await _announcementService.GetGroupAnnouncementsAsync(query.GroupId, query.UserId, query.UserRole, cancellationToken);
            return Result<List<AnnouncementResponseDto>>.Success(result);
        }
    }
}