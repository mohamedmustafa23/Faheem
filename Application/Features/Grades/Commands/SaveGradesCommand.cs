using Application.Features.Grades.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Grades.Commands
{
    public class SaveGradesCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public SaveGradesRequest Request { get; set; } = new();
    }

    public class SaveGradesCommandHandler : IRequestHandler<SaveGradesCommand, Result>
    {
        private readonly IGradeService _gradeService;
        public SaveGradesCommandHandler(IGradeService gradeService) => _gradeService = gradeService;

        public async Task<Result> Handle(SaveGradesCommand command, CancellationToken cancellationToken)
        {
            var result = await _gradeService.SaveGradesAsync(command.Request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}