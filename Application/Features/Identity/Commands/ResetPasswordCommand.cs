using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Commands
{
    public class ResetPasswordCommand : IRequest<Result>
    {
        public ResetPasswordRequest Request { get; set; } = new();
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly IAuthService _authService;
        public ResetPasswordCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            var result = await _authService.ResetPasswordAsync(command.Request, cancellationToken);
            return Result<string>.Success(result, "Password has been reset successfully.");
        }
    }
}