using Application.Features.LessonReports.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.LessonReports.Queries
{
    // Student: my own lesson reports in one group, newest first. Enrollment is
    // enforced inside the service.
    public class GetStudentGroupReportsQuery : IRequest<Result<List<ChildLessonReportDto>>>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
    }

    public class GetStudentGroupReportsQueryHandler : IRequestHandler<GetStudentGroupReportsQuery, Result<List<ChildLessonReportDto>>>
    {
        private readonly ILessonReportService _lessonReportService;
        public GetStudentGroupReportsQueryHandler(ILessonReportService lessonReportService) => _lessonReportService = lessonReportService;

        public async Task<Result<List<ChildLessonReportDto>>> Handle(GetStudentGroupReportsQuery query, CancellationToken cancellationToken)
        {
            var data = await _lessonReportService.GetStudentGroupReportsAsync(query.StudentId, query.GroupId, cancellationToken);
            return Result<List<ChildLessonReportDto>>.Success(data);
        }
    }
}
