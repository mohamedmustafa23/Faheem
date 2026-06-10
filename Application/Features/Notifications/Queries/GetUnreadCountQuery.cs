using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Notifications.Queries
{
    public class GetUnreadCountQuery : IRequest<Result<int>>
    {
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
    }

    public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
    {
        private readonly INotificationService _notificationService;
        public GetUnreadCountQueryHandler(INotificationService notificationService) =>
            _notificationService = notificationService;

        public async Task<Result<int>> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken)
        {
            var count = await _notificationService.GetUnreadCountAsync(query.UserId, cancellationToken);
            return Result<int>.Success(count);
        }
    }
}
