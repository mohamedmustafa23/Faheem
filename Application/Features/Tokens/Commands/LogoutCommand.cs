using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Tokens.Commands
{
    public class LogoutCommand : IRequest<Result>
    {
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// FCM token for this device. Optional — when present, the device is
        /// deactivated atomically with the refresh-token revoke so the user
        /// can't receive any further push after logout, even if a separate
        /// "unregister token" call would have failed (network, race, crash).
        /// </summary>
        public string? FcmToken { get; set; }
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
    {
        private readonly ITokenService _tokenService;
        private readonly INotificationService _notificationService;

        public LogoutCommandHandler(ITokenService tokenService, INotificationService notificationService)
        {
            _tokenService = tokenService;
            _notificationService = notificationService;
        }

        public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
        {
            // Order matters: revoke first so even if the FCM deactivate throws,
            // the session is already gone. Push deactivation is best-effort.
            await _tokenService.LogoutAsync(command.UserId, command.RefreshToken);

            if (!string.IsNullOrWhiteSpace(command.FcmToken))
            {
                try
                {
                    await _notificationService.DeleteDeviceTokenAsync(command.FcmToken, command.UserId, cancellationToken);
                }
                catch
                {
                    // Swallow — logout already succeeded; a residual FCM row at
                    // worst delivers one stray push that the client (now signed
                    // out) will ignore. We don't want to surface a partial-
                    // failure error and confuse the user.
                }
            }

            return Result.Success("Logged out successfully from this device.");
        }
    }
}