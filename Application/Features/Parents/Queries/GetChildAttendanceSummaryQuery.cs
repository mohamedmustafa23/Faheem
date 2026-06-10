using Application.Exceptions;
using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Parents.Queries
{
    /// <summary>
    /// Per-group attendance summary for one of the parent's linked children.
    /// Delegates to the student-facing summary after a parent-link guard.
    /// </summary>
    public class GetChildAttendanceSummaryQuery : IRequest<Result<List<MyGroupAttendanceDto>>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string ChildId { get; set; } = string.Empty;
    }

    public class GetChildAttendanceSummaryQueryHandler : IRequestHandler<GetChildAttendanceSummaryQuery, Result<List<MyGroupAttendanceDto>>>
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IParentService _parentService;

        public GetChildAttendanceSummaryQueryHandler(IAttendanceService attendanceService, IParentService parentService)
        {
            _attendanceService = attendanceService;
            _parentService = parentService;
        }

        public async Task<Result<List<MyGroupAttendanceDto>>> Handle(GetChildAttendanceSummaryQuery query, CancellationToken cancellationToken)
        {
            var isLinked = await _parentService.IsParentLinkedToChildAsync(query.ParentId, query.ChildId, cancellationToken);
            if (!isLinked) throw new ForbiddenException(["You do not have access to this student's attendance."]);

            var data = await _attendanceService.GetMyAttendanceSummaryAsync(query.ChildId, cancellationToken);
            return Result<List<MyGroupAttendanceDto>>.Success(data);
        }
    }
}
