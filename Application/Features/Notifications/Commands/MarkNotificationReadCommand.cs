using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Notifications.Commands
{
    /// <summary>
    /// Flips one notification to read — used when the user taps it in the in-app
    /// list. Scoped to the owner; idempotent (returns 0 if already read/not theirs).
    /// </summary>
    public class MarkNotificationReadCommand : IRequest<Result<int>>
    {
        public Guid NotificationId { get; set; }
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
    }

    public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result<int>>
    {
        private readonly INotificationService _notificationService;
        public MarkNotificationReadCommandHandler(INotificationService notificationService) => _notificationService = notificationService;

        public async Task<Result<int>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        {
            var updated = await _notificationService.MarkAsReadAsync(request.NotificationId, request.UserId, cancellationToken);
            return Result<int>.Success(updated);
        }
    }
}
