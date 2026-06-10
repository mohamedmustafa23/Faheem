using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Commands
{
    public class ForgotPasswordCommand : IRequest<Result>
    {
        public ForgotPasswordRequest Request { get; set; } = new();
    }

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
    {
        private readonly IAuthService _authService;
        public ForgotPasswordCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
        {
            await _authService.ForgotPasswordAsync(command.Request.Email, cancellationToken);
            return Result.Success( "Password reset code sent to your email.");
        }
    }
}