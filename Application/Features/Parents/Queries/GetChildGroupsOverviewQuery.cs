using Application.Exceptions;
using Application.Features.Parents.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Parents.Queries
{
    // Group-centric overview for one linked child: per group, the child's rank,
    // attendance, grades, and fees. Guarded by the parent-link check.
    public class GetChildGroupsOverviewQuery : IRequest<Result<List<ChildGroupOverviewDto>>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string ChildId { get; set; } = string.Empty;
    }

    public class GetChildGroupsOverviewQueryHandler : IRequestHandler<GetChildGroupsOverviewQuery, Result<List<ChildGroupOverviewDto>>>
    {
        private readonly IParentInsightsService _insights;
        private readonly IParentService _parentService;

        public GetChildGroupsOverviewQueryHandler(IParentInsightsService insights, IParentService parentService)
        {
            _insights = insights;
            _parentService = parentService;
        }

        public async Task<Result<List<ChildGroupOverviewDto>>> Handle(GetChildGroupsOverviewQuery query, CancellationToken cancellationToken)
        {
            var isLinked = await _parentService.IsParentLinkedToChildAsync(query.ParentId, query.ChildId, cancellationToken);
            if (!isLinked) throw new ForbiddenException(["You do not have access to this student."]);

            var data = await _insights.GetChildGroupsOverviewAsync(query.ChildId, cancellationToken);
            return Result<List<ChildGroupOverviewDto>>.Success(data);
        }
    }
}
