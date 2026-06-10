using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Commands
{
    public class SendOtpCommand : IRequest<Result>
    {
        public SendOtpRequest Request { get; set; } = new();
    }

    public class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, Result>
    {
        private readonly IAuthService _authService;
        public SendOtpCommandHandler(IAuthService authService) => _authService = authService;

        public async Task<Result> Handle(SendOtpCommand command, CancellationToken cancellationToken)
        {
            await _authService.GenerateAndSendOtpAsync(command.Request.Email, cancellationToken);
            return Result.Success("OTP sent successfully to your email.");
        }
    }
}