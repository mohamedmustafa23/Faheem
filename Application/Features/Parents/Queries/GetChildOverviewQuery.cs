using Application.Features.Parents.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Parents.Queries
{
    /// <summary>
    /// Returns the parent dashboard card / Overview tab snapshot in one round-trip.
    /// Authorization is enforced inside ParentService.GetChildOverviewAsync.
    /// </summary>
    public class GetChildOverviewQuery : IRequest<Result<ChildOverviewDto>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string ChildId { get; set; } = string.Empty;
        public DateOnly? Today { get; set; }
    }

    public class GetChildOverviewQueryHandler : IRequestHandler<GetChildOverviewQuery, Result<ChildOverviewDto>>
    {
        private readonly IParentService _parentService;
        private readonly IDateTimeService _dateTime;
        public GetChildOverviewQueryHandler(IParentService parentService, IDateTimeService dateTime)
        {
            _parentService = parentService;
            _dateTime = dateTime;
        }

        public async Task<Result<ChildOverviewDto>> Handle(GetChildOverviewQuery query, CancellationToken cancellationToken)
        {
            var today = query.Today ?? _dateTime.TodayInAppZone;
            var data = await _parentService.GetChildOverviewAsync(query.ParentId, query.ChildId, today, cancellationToken);
            return Result<ChildOverviewDto>.Success(data);
        }
    }
}
