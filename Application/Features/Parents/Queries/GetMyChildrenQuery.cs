using Application.Features.Parents.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Parents.Queries
{
    public class GetMyChildrenQuery : IRequest<Result<List<LinkedChildDto>>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
    }

    public class GetChildDetailsQuery : IRequest<Result<ChildDetailsDto>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string ChildId { get; set; } = string.Empty;
        public DateOnly? Today { get; set; }
    }

    public class ParentQueriesHandler :
        IRequestHandler<GetMyChildrenQuery, Result<List<LinkedChildDto>>>,
        IRequestHandler<GetChildDetailsQuery, Result<ChildDetailsDto>>
    {
        private readonly IParentService _parentService;
        private readonly IDateTimeService _dateTime;
        public ParentQueriesHandler(IParentService parentService, IDateTimeService dateTime)
        {
            _parentService = parentService;
            _dateTime = dateTime;
        }

        public async Task<Result<List<LinkedChildDto>>> Handle(GetMyChildrenQuery request, CancellationToken ct)
            => Result<List<LinkedChildDto>>.Success(await _parentService.GetMyChildrenAsync(request.ParentId, ct));

        public async Task<Result<ChildDetailsDto>> Handle(GetChildDetailsQuery request, CancellationToken ct)
        {
            var today = request.Today ?? _dateTime.TodayInAppZone;
            return Result<ChildDetailsDto>.Success(await _parentService.GetChildDetailsAsync(request.ParentId, request.ChildId, today, ct));
        }
    }
}
