using Application.Features.Grades.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Grades.Commands
{
    public class CreateExamCommand : IRequest<Result<Guid>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public CreateExamRequest Request { get; set; } = new();
    }

    public class CreateExamCommandHandler : IRequestHandler<CreateExamCommand, Result<Guid>>
    {
        private readonly IGradeService _gradeService;
        public CreateExamCommandHandler(IGradeService gradeService) => _gradeService = gradeService;

        public async Task<Result<Guid>> Handle(CreateExamCommand command, CancellationToken cancellationToken)
        {
            var examId = await _gradeService.CreateExamAsync(command.Request, command.TenantId, cancellationToken);
            return Result<Guid>.Success(examId, "Exam created successfully.");
        }
    }
}