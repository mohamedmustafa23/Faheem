using Application.Features.Notifications.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Notifications.Commands
{
    public class SaveDeviceTokenCommand : IRequest<Result>
    {
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
        public SaveDeviceTokenRequest Request { get; set; } = new();
    }

    public class SaveDeviceTokenCommandHandler : IRequestHandler<SaveDeviceTokenCommand, Result>
    {
        private readonly INotificationService _notificationService;
        public SaveDeviceTokenCommandHandler(INotificationService notificationService) => _notificationService = notificationService;

        public async Task<Result> Handle(SaveDeviceTokenCommand command, CancellationToken cancellationToken)
        {
            var result = await _notificationService.SaveDeviceTokenAsync(command.Request, command.UserId, cancellationToken);
            return Result.Success(result);
        }
    }
}