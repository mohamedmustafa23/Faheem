using Application.Features.Groups.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Groups.Commands
{
    public class EditStudentCommand : IRequest<Result>
    {
        [JsonIgnore] public Guid GroupId { get; set; }
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public EditStudentRequest Request { get; set; } = new();
    }

    public class EditStudentCommandHandler : IRequestHandler<EditStudentCommand, Result>
    {
        private readonly IEnrollmentService _enrollmentService;

        public EditStudentCommandHandler(IEnrollmentService enrollmentService)
            => _enrollmentService = enrollmentService;

        public async Task<Result> Handle(EditStudentCommand command, CancellationToken cancellationToken)
        {
            var result = await _enrollmentService.EditGhostStudentAsync(command.GroupId, command.StudentId, command.Request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
