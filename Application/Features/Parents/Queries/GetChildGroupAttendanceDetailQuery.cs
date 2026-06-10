using Application.Exceptions;
using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Parents.Queries
{
    /// <summary>
    /// Full attendance history for one of the parent's linked children in a
    /// specific group — drives the calendar heatmap inside the parent's
    /// child detail "Attendance" tab.
    /// </summary>
    public class GetChildGroupAttendanceDetailQuery : IRequest<Result<MyGroupAttendanceDto>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string ChildId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
    }

    public class GetChildGroupAttendanceDetailQueryHandler : IRequestHandler<GetChildGroupAttendanceDetailQuery, Result<MyGroupAttendanceDto>>
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IParentService _parentService;

        public GetChildGroupAttendanceDetailQueryHandler(IAttendanceService attendanceService, IParentService parentService)
        {
            _attendanceService = attendanceService;
            _parentService = parentService;
        }

        public async Task<Result<MyGroupAttendanceDto>> Handle(GetChildGroupAttendanceDetailQuery query, CancellationToken cancellationToken)
        {
            var isLinked = await _parentService.IsParentLinkedToChildAsync(query.ParentId, query.ChildId, cancellationToken);
            if (!isLinked) throw new ForbiddenException(["You do not have access to this student's attendance."]);

            var data = await _attendanceService.GetMyGroupAttendanceDetailAsync(query.ChildId, query.GroupId, cancellationToken);
            return Result<MyGroupAttendanceDto>.Success(data);
        }
    }
}
