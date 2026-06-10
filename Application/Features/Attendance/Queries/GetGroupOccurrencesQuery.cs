using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Queries
{
    public class GetGroupOccurrencesQuery : IRequest<Result<PaginatedResult<GroupOccurrenceDto>>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetGroupOccurrencesQueryHandler : IRequestHandler<GetGroupOccurrencesQuery, Result<PaginatedResult<GroupOccurrenceDto>>>
    {
        private readonly IAttendanceService _attendanceService;
        public GetGroupOccurrencesQueryHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result<PaginatedResult<GroupOccurrenceDto>>> Handle(GetGroupOccurrencesQuery query, CancellationToken cancellationToken)
        {
            var result = await _attendanceService.GetGroupOccurrencesAsync(query.GroupId, query.Page, query.PageSize, query.TenantId, cancellationToken);
            return Result<PaginatedResult<GroupOccurrenceDto>>.Success(result);
        }
    }
}
