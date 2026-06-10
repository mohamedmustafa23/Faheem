using Application.Features.Students.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Students.Queries
{
    public class GetStudentGroupsQuery : IRequest<Result<List<StudentGroupDto>>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
    }

    public class GetStudentTodayScheduleQuery : IRequest<Result<List<StudentTodaySessionDto>>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
        public DateOnly? Today { get; set; }
    }

    public class GetPendingParentRequestsQuery : IRequest<Result<List<PendingParentRequestDto>>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
    }

    public class StudentQueriesHandler :
        IRequestHandler<GetStudentGroupsQuery, Result<List<StudentGroupDto>>>,
        IRequestHandler<GetStudentTodayScheduleQuery, Result<List<StudentTodaySessionDto>>>,
        IRequestHandler<GetPendingParentRequestsQuery, Result<List<PendingParentRequestDto>>>
    {
        private readonly IStudentService _studentService;
        private readonly IDateTimeService _dateTime;
        public StudentQueriesHandler(IStudentService studentService, IDateTimeService dateTime)
        {
            _studentService = studentService;
            _dateTime = dateTime;
        }

        public async Task<Result<List<StudentGroupDto>>> Handle(GetStudentGroupsQuery request, CancellationToken ct)
            => Result<List<StudentGroupDto>>.Success(await _studentService.GetMyGroupsAsync(request.StudentId, ct));

        public async Task<Result<List<StudentTodaySessionDto>>> Handle(GetStudentTodayScheduleQuery request, CancellationToken ct)
        {
            var today = request.Today ?? _dateTime.TodayInAppZone;
            return Result<List<StudentTodaySessionDto>>.Success(await _studentService.GetMyTodayScheduleAsync(request.StudentId, today, ct));
        }

        public async Task<Result<List<PendingParentRequestDto>>> Handle(GetPendingParentRequestsQuery request, CancellationToken ct)
            => Result<List<PendingParentRequestDto>>.Success(await _studentService.GetPendingParentRequestsAsync(request.StudentId, ct));
    }
}
