using Application.Features.Parents.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Parents.Queries
{
    /// <summary>
    /// Aggregated announcements across every group the parent's child is in.
    /// Authorization is enforced inside ParentService.GetChildAnnouncementsAsync.
    /// </summary>
    public class GetChildAnnouncementsQuery : IRequest<Result<List<ChildAnnouncementDto>>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string ChildId { get; set; } = string.Empty;
    }

    public class GetChildAnnouncementsQueryHandler : IRequestHandler<GetChildAnnouncementsQuery, Result<List<ChildAnnouncementDto>>>
    {
        private readonly IParentService _parentService;
        public GetChildAnnouncementsQueryHandler(IParentService parentService) => _parentService = parentService;

        public async Task<Result<List<ChildAnnouncementDto>>> Handle(GetChildAnnouncementsQuery query, CancellationToken cancellationToken)
        {
            var data = await _parentService.GetChildAnnouncementsAsync(query.ParentId, query.ChildId, cancellationToken);
            return Result<List<ChildAnnouncementDto>>.Success(data);
        }
    }
}
