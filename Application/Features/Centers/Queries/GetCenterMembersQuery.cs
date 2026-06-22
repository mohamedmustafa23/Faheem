using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Queries
{
    public class GetCenterMembersQuery : IRequest<Result<List<CenterMemberDto>>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public string OwnerUserId { get; set; } = string.Empty;
    }

    public class GetCenterMembersQueryHandler : IRequestHandler<GetCenterMembersQuery, Result<List<CenterMemberDto>>>
    {
        private readonly ICenterService _centerService;
        public GetCenterMembersQueryHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<List<CenterMemberDto>>> Handle(GetCenterMembersQuery query, CancellationToken cancellationToken)
        {
            var members = await _centerService.GetCenterMembersAsync(query.TenantId, query.OwnerUserId, cancellationToken);
            return Result<List<CenterMemberDto>>.Success(members);
        }
    }
}
