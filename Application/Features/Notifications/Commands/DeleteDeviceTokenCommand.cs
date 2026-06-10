using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Notifications.Commands
{
    /// <summary>
    /// Unregisters an FCM token for the current user — called from the client
    /// on logout so the device stops receiving notifications for the prior user.
    /// </summary>
    public class DeleteDeviceTokenCommand : IRequest<Result>
    {
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
        public string FcmToken { get; set; } = string.Empty;
    }

    public class DeleteDeviceTokenCommandHandler : IRequestHandler<DeleteDeviceTokenCommand, Result>
    {
        private readonly INotificationService _notificationService;
        public DeleteDeviceTokenCommandHandler(INotificationService notificationService) => _notificationService = notificationService;

        public async Task<Result> Handle(DeleteDeviceTokenCommand command, CancellationToken cancellationToken)
        {
            var result = await _notificationService.DeleteDeviceTokenAsync(command.FcmToken, command.UserId, cancellationToken);
            return Result.Success(result);
        }
    }
}
