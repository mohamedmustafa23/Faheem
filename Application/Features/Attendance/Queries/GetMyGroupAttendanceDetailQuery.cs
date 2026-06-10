using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Queries
{
    public class GetMyGroupAttendanceDetailQuery : IRequest<Result<MyGroupAttendanceDto>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
    }

    public class GetMyGroupAttendanceDetailQueryHandler : IRequestHandler<GetMyGroupAttendanceDetailQuery, Result<MyGroupAttendanceDto>>
    {
        private readonly IAttendanceService _attendanceService;
        public GetMyGroupAttendanceDetailQueryHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result<MyGroupAttendanceDto>> Handle(GetMyGroupAttendanceDetailQuery query, CancellationToken cancellationToken)
        {
            var result = await _attendanceService.GetMyGroupAttendanceDetailAsync(query.StudentId, query.GroupId, cancellationToken);
            return Result<MyGroupAttendanceDto>.Success(result);
        }
    }
}
