using Application.Features.LessonReports.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.LessonReports.Commands
{
    public class SaveLessonReportCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public SaveLessonReportRequest Request { get; set; } = new();
    }

    public class SaveLessonReportCommandHandler : IRequestHandler<SaveLessonReportCommand, Result>
    {
        private readonly ILessonReportService _lessonReportService;
        public SaveLessonReportCommandHandler(ILessonReportService lessonReportService) => _lessonReportService = lessonReportService;

        public async Task<Result> Handle(SaveLessonReportCommand command, CancellationToken cancellationToken)
        {
            var result = await _lessonReportService.SaveReportAsync(command.Request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
