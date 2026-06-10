using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Queries
{
    /// <summary>
    /// Returns the student's current consecutive-Present streak count across
    /// every group they're enrolled in. Purely cosmetic for the dashboard.
    /// </summary>
    public class GetMyAttendanceStreakQuery : IRequest<Result<int>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
    }

    public class GetMyAttendanceStreakQueryHandler : IRequestHandler<GetMyAttendanceStreakQuery, Result<int>>
    {
        private readonly IAttendanceService _attendanceService;
        public GetMyAttendanceStreakQueryHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result<int>> Handle(GetMyAttendanceStreakQuery query, CancellationToken cancellationToken)
        {
            var streak = await _attendanceService.GetMyAttendanceStreakAsync(query.StudentId, cancellationToken);
            return Result<int>.Success(streak);
        }
    }
}
