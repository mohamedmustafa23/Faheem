using Application.Features.Groups.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using FluentValidation;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Enrollment.Commands
{
    public class JoinGroupCommand : IRequest<Result>
    {
        [JsonIgnore]
        public string StudentId { get; set; } = string.Empty;

        public string EnrollmentCode { get; set; } = string.Empty;
    }

    public class JoinGroupCommandValidator : AbstractValidator<JoinGroupCommand>
    {
        public JoinGroupCommandValidator()
        {
            RuleFor(x => x.EnrollmentCode).NotEmpty().Length(6);
        }
    }

    public class JoinGroupCommandHandler : IRequestHandler<JoinGroupCommand, Result>
    {
        private readonly IEnrollmentService _enrollmentService;

        public JoinGroupCommandHandler(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        public async Task<Result> Handle(JoinGroupCommand command, CancellationToken cancellationToken)
        {
            var message = await _enrollmentService.JoinGroupAsync(command.StudentId, command.EnrollmentCode, cancellationToken);

            return Result.Success(message);
        }
    }
}