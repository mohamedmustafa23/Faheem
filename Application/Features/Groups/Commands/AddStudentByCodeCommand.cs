using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Groups.Commands
{
    public class AddStudentByCodeCommand : IRequest<Result>
    {
        [JsonIgnore] public Guid GroupId { get; set; }
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
    }

    public class AddStudentByCodeCommandHandler : IRequestHandler<AddStudentByCodeCommand, Result>
    {
        private readonly IEnrollmentService _enrollmentService;

        public AddStudentByCodeCommandHandler(IEnrollmentService enrollmentService)
            => _enrollmentService = enrollmentService;

        public async Task<Result> Handle(AddStudentByCodeCommand command, CancellationToken cancellationToken)
        {
            var result = await _enrollmentService.AddStudentByCodeAsync(command.GroupId, command.StudentCode, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
