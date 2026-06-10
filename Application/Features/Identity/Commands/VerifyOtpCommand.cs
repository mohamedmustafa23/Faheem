using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Commands
{
    public class VerifyOtpCommand : IRequest<Result>
    {
        public VerifyOtpRequest Request { get; set; } = new();
    }

    public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result>
    {
        private readonly IAuthService _authService;
        public VerifyOtpCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result> Handle(VerifyOtpCommand command, CancellationToken cancellationToken)
        {
            var result = await _authService.VerifyOtpAsync(command.Request, cancellationToken);
            return Result<string>.Success(result, "Email verified successfully.");
        }
    }
}