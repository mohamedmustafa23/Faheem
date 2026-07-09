using Application.Features.LessonReports.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.LessonReports.Queries
{
    // Teacher: load the editor (present students + any saved report) for one occurrence.
    public class GetOccurrenceReportQuery : IRequest<Result<LessonReportEditorDto>>
    {
        public Guid OccurrenceId { get; set; }
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
    }

    public class GetOccurrenceReportQueryHandler : IRequestHandler<GetOccurrenceReportQuery, Result<LessonReportEditorDto>>
    {
        private readonly ILessonReportService _lessonReportService;
        public GetOccurrenceReportQueryHandler(ILessonReportService lessonReportService) => _lessonReportService = lessonReportService;

        public async Task<Result<LessonReportEditorDto>> Handle(GetOccurrenceReportQuery query, CancellationToken cancellationToken)
        {
            var data = await _lessonReportService.GetOccurrenceReportAsync(query.OccurrenceId, query.TenantId, cancellationToken);
            return Result<LessonReportEditorDto>.Success(data);
        }
    }
}
