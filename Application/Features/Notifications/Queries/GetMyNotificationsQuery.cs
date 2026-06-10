using Application.Features.Notifications.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Notifications.Queries
{
    public class GetMyNotificationsQuery : IRequest<Result<PaginatedResult<NotificationResponseDto>>>
    {
        [JsonIgnore] public string UserId { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        /// <summary>When false, the call leaves IsRead untouched — safe for dashboard previews.</summary>
        public bool MarkAsRead { get; set; } = true;
    }

    public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, Result<PaginatedResult<NotificationResponseDto>>>
    {
        private readonly INotificationService _notificationService;
        public GetMyNotificationsQueryHandler(INotificationService notificationService) => _notificationService = notificationService;

        public async Task<Result<PaginatedResult<NotificationResponseDto>>> Handle(GetMyNotificationsQuery query, CancellationToken cancellationToken)
        {
            var result = await _notificationService.GetMyNotificationsAsync(query.UserId, query.Page, query.PageSize, query.MarkAsRead, cancellationToken);
            return Result<PaginatedResult<NotificationResponseDto>>.Success(result);
        }
    }
}
