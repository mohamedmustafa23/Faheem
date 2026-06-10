using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Commands
{
    public class RegisterTeacherCommand : IRequest<Result<string>>
    {
        public RegisterTeacherRequest Request { get; set; } = new();
    }
    public class RegisterTeacherCommandHandler : IRequestHandler<RegisterTeacherCommand, Result<string>>
    {
        private readonly IAuthService _authService;
        public RegisterTeacherCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result<string>> Handle(RegisterTeacherCommand command, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterTeacherAsync(command.Request, cancellationToken);
            return Result<string>.Success(result, "Teacher registered successfully. Account pending activation.");
        }
    }
}
