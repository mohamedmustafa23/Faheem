using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Commands
{
    public class RegisterStudentCommand : IRequest<Result<string>>
    {
        public RegisterStudentRequest Request { get; set; } = new();
    }
    public class RegisterStudentCommandHandler : IRequestHandler<RegisterStudentCommand, Result<string>>
    {
        private readonly IAuthService _authService;
        public RegisterStudentCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result<string>> Handle(RegisterStudentCommand command, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterStudentAsync(command.Request, cancellationToken);
            return Result<string>.Success(result, "Student registered successfully. Please verify your email.");
        }
    }
}
