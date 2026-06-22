using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Queries
{
    public class GetMyInvitesQuery : IRequest<Result<List<PendingInviteDto>>>
    {
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
    }

    public class GetMyInvitesQueryHandler : IRequestHandler<GetMyInvitesQuery, Result<List<PendingInviteDto>>>
    {
        private readonly ICenterService _centerService;
        public GetMyInvitesQueryHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<List<PendingInviteDto>>> Handle(GetMyInvitesQuery query, CancellationToken cancellationToken)
        {
            var invites = await _centerService.GetMyInvitesAsync(query.UserId, cancellationToken);
            return Result<List<PendingInviteDto>>.Success(invites);
        }
    }
}
