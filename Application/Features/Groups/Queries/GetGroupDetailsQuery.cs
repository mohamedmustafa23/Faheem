using Application.Features.Groups.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Groups.Queries
{
    public class GetGroupDetailsQuery : IRequest<Result<GroupDetailsResponseDto>>
    {
        [JsonIgnore]
        public string TenantId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
    }

    public class GetGroupDetailsQueryHandler : IRequestHandler<GetGroupDetailsQuery, Result<GroupDetailsResponseDto>>
    {
        private readonly IGroupService _groupService;
        public GetGroupDetailsQueryHandler(IGroupService groupService) => _groupService = groupService;

        public async Task<Result<GroupDetailsResponseDto>> Handle(GetGroupDetailsQuery query, CancellationToken cancellationToken)
        {
            var groupDetails = await _groupService.GetGroupDetailsAsync(query.GroupId, query.TenantId, cancellationToken);
            return Result<GroupDetailsResponseDto>.Success(groupDetails);
        }
    }
}