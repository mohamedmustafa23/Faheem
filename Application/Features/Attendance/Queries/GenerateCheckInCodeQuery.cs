using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Queries
{
    public class GenerateCheckInCodeQuery : IRequest<Result<CheckInCodeDto>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
        public Guid OccurrenceId { get; set; }
    }

    public class GenerateCheckInCodeQueryHandler : IRequestHandler<GenerateCheckInCodeQuery, Result<CheckInCodeDto>>
    {
        private readonly IAttendanceService _attendanceService;
        public GenerateCheckInCodeQueryHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result<CheckInCodeDto>> Handle(GenerateCheckInCodeQuery query, CancellationToken cancellationToken)
        {
            var data = await _attendanceService.GenerateCheckInTokenAsync(query.StudentId, query.OccurrenceId, cancellationToken);
            return Result<CheckInCodeDto>.Success(data);
        }
    }
}
