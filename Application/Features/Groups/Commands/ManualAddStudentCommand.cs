using Application.Features.Groups.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Groups.Commands
{
    // Returns the generated StudentCode so the teacher can hand it to the student / parent.
    public class ManualAddStudentCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;

        [JsonIgnore] public Guid GroupId { get; set; }
        public ManualAddStudentBodyRequest Request { get; set; } = new();
    }

    public class ManualAddStudentCommandHandler : IRequestHandler<ManualAddStudentCommand, Result<string>>
    {
        private readonly IEnrollmentService _enrollmentService;

        public ManualAddStudentCommandHandler(IEnrollmentService enrollmentService)
            => _enrollmentService = enrollmentService;

        public async Task<Result<string>> Handle(ManualAddStudentCommand command, CancellationToken cancellationToken)
        {
            var internalRequest = new ManualAddStudentRequest
            {
                GroupId = command.GroupId,
                FirstName = command.Request.FirstName,
                LastName = command.Request.LastName,
                EducationalStage = command.Request.EducationalStage,
                GradeYear = command.Request.GradeYear,
                ParentPhoneNumber = command.Request.ParentPhoneNumber
            };

            var studentCode = await _enrollmentService.ManualAddStudentAsync(internalRequest, command.TenantId, cancellationToken);
            return Result<string>.Success(studentCode);
        }
    }
}
