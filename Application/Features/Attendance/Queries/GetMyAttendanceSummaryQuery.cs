using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Queries
{
    public class GetMyAttendanceSummaryQuery : IRequest<Result<List<MyGroupAttendanceDto>>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
    }

    public class GetMyAttendanceSummaryQueryHandler : IRequestHandler<GetMyAttendanceSummaryQuery, Result<List<MyGroupAttendanceDto>>>
    {
        private readonly IAttendanceService _attendanceService;
        public GetMyAttendanceSummaryQueryHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result<List<MyGroupAttendanceDto>>> Handle(GetMyAttendanceSummaryQuery query, CancellationToken cancellationToken)
        {
            var result = await _attendanceService.GetMyAttendanceSummaryAsync(query.StudentId, cancellationToken);
            return Result<List<MyGroupAttendanceDto>>.Success(result);
        }
    }
}
