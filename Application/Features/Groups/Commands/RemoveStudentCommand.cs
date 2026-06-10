using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Groups.Commands
{
    public class RemoveStudentCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public string StudentId { get; set; } = string.Empty;
    }

    public class RemoveStudentCommandHandler : IRequestHandler<RemoveStudentCommand, Result>
    {
        private readonly IEnrollmentService _enrollmentService;
        public RemoveStudentCommandHandler(IEnrollmentService enrollmentService) => _enrollmentService = enrollmentService;

        public async Task<Result> Handle(RemoveStudentCommand command, CancellationToken cancellationToken)
        {
            var result = await _enrollmentService.RemoveStudentAsync(command.GroupId, command.StudentId, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
