using Application.Exceptions;
using Application.Features.LessonReports.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.LessonReports.Queries
{
    // Parent: the child's lesson reports in one group. Guarded by the parent-link check.
    public class GetChildGroupReportsQuery : IRequest<Result<List<ChildLessonReportDto>>>
    {
        [JsonIgnore] public string ParentId { get; set; } = string.Empty;
        public string ChildId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
    }

    public class GetChildGroupReportsQueryHandler : IRequestHandler<GetChildGroupReportsQuery, Result<List<ChildLessonReportDto>>>
    {
        private readonly ILessonReportService _lessonReportService;
        private readonly IParentService _parentService;

        public GetChildGroupReportsQueryHandler(ILessonReportService lessonReportService, IParentService parentService)
        {
            _lessonReportService = lessonReportService;
            _parentService = parentService;
        }

        public async Task<Result<List<ChildLessonReportDto>>> Handle(GetChildGroupReportsQuery query, CancellationToken cancellationToken)
        {
            var isLinked = await _parentService.IsParentLinkedToChildAsync(query.ParentId, query.ChildId, cancellationToken);
            if (!isLinked) throw new ForbiddenException(["You do not have access to this student."]);

            var data = await _lessonReportService.GetChildGroupReportsAsync(query.ChildId, query.GroupId, cancellationToken);
            return Result<List<ChildLessonReportDto>>.Success(data);
        }
    }
}
