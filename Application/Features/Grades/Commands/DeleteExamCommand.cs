using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Grades.Commands
{
    public class DeleteExamCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid ExamId { get; set; }
    }

    public class DeleteExamCommandHandler : IRequestHandler<DeleteExamCommand, Result<string>>
    {
        private readonly IGradeService _gradeService;
        public DeleteExamCommandHandler(IGradeService gradeService) => _gradeService = gradeService;

        public async Task<Result<string>> Handle(DeleteExamCommand command, CancellationToken cancellationToken)
        {
            var result = await _gradeService.DeleteExamAsync(command.ExamId, command.TenantId, cancellationToken);
            return Result<string>.Success(result);
        }
    }
}
