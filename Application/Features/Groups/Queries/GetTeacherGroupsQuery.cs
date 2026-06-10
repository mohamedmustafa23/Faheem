using Application.Features.Groups.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Groups.Queries
{
    public class GetTeacherGroupsQuery : IRequest<Result<List<GroupResponseDto>>>
    {
        [JsonIgnore]
        public string TenantId { get; set; } = string.Empty; 
    }

    public class GetTeacherGroupsQueryHandler : IRequestHandler<GetTeacherGroupsQuery, Result<List<GroupResponseDto>>>
    {
        private readonly IGroupService _groupService;

        public GetTeacherGroupsQueryHandler(IGroupService groupService)
        {
            _groupService = groupService;
        }

        public async Task<Result<List<GroupResponseDto>>> Handle(GetTeacherGroupsQuery query, CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetTeacherGroupsAsync(query.TenantId, cancellationToken);
            return Result<List<GroupResponseDto>>.Success(groups);
        }
    }
}