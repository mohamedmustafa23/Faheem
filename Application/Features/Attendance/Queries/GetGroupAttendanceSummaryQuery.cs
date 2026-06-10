using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Queries
{
    public class GetGroupAttendanceSummaryQuery : IRequest<Result<List<StudentAttendanceSummaryDto>>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
    }

    public class GetGroupAttendanceSummaryQueryHandler : IRequestHandler<GetGroupAttendanceSummaryQuery, Result<List<StudentAttendanceSummaryDto>>>
    {
        private readonly IAttendanceService _attendanceService;
        public GetGroupAttendanceSummaryQueryHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result<List<StudentAttendanceSummaryDto>>> Handle(GetGroupAttendanceSummaryQuery query, CancellationToken cancellationToken)
        {
            var result = await _attendanceService.GetGroupAttendanceSummaryAsync(query.GroupId, query.TenantId, cancellationToken);
            return Result<List<StudentAttendanceSummaryDto>>.Success(result);
        }
    }
}
