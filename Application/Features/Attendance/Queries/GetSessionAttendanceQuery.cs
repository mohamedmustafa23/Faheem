using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Queries
{
    public class GetSessionAttendanceQuery : IRequest<Result<List<StudentAttendanceDto>>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid OccurrenceId { get; set; }
    }

    public class GetSessionAttendanceQueryHandler : IRequestHandler<GetSessionAttendanceQuery, Result<List<StudentAttendanceDto>>>
    {
        private readonly IAttendanceService _attendanceService;
        public GetSessionAttendanceQueryHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result<List<StudentAttendanceDto>>> Handle(GetSessionAttendanceQuery query, CancellationToken cancellationToken)
        {
            var result = await _attendanceService.GetOccurrenceAttendanceAsync(query.OccurrenceId, query.TenantId, cancellationToken);
            return Result<List<StudentAttendanceDto>>.Success(result);
        }
    }
}
