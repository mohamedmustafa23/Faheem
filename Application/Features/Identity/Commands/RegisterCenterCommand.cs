using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Commands
{
    public class RegisterCenterCommand : IRequest<Result<string>>
    {
        public RegisterCenterRequest Request { get; set; } = new();
    }

    public class RegisterCenterCommandHandler : IRequestHandler<RegisterCenterCommand, Result<string>>
    {
        private readonly IAuthService _authService;
        public RegisterCenterCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result<string>> Handle(RegisterCenterCommand command, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterCenterAsync(command.Request, cancellationToken);
            return Result<string>.Success(result, "Center registered successfully. Verify your account to start your 1-month trial.");
        }
    }
}
