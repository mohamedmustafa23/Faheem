using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Notifications.Commands
{
    /// <summary>
    /// Flips every unread notification for the current user to read. Idempotent —
    /// callable any time and returns the count of rows updated.
    /// </summary>
    public class MarkAllNotificationsReadCommand : IRequest<Result<int>>
    {
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
    }

    public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result<int>>
    {
        private readonly INotificationService _notificationService;
        public MarkAllNotificationsReadCommandHandler(INotificationService notificationService) => _notificationService = notificationService;

        public async Task<Result<int>> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
        {
            var updated = await _notificationService.MarkAllAsReadAsync(request.UserId, cancellationToken);
            return Result<int>.Success(updated);
        }
    }
}
