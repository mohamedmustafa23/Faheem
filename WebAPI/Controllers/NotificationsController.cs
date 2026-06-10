using Application.Features.Notifications.Commands;
using Application.Features.Notifications.DTOs;
using Application.Features.Notifications.Queries;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers
{
    [Route("api/notifications")]
    [Authorize] 
    [OpenApiTag("Shared - Notifications", Description = "Endpoints for viewing notifications and saving device tokens")]
    public class NotificationsController : BaseApiController
    {
        [HttpGet]
        [OpenApiOperation("Get My Notifications", "Retrieves notifications for the logged-in user (paginated, 20 per page). Marks fetched notifications as read unless markAsRead=false is passed (useful for dashboard previews that shouldn't consume the unread state).")]
        public async Task<IActionResult> GetMyNotificationsAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool markAsRead = true)
        {
            var query = new GetMyNotificationsQuery
            {
                UserId     = User.GetUserId()!,
                Page       = page,
                PageSize   = pageSize,
                MarkAsRead = markAsRead
            };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("unread-count")]
        [OpenApiOperation("Get Unread Count", "Returns the number of unread notifications for the current user — safe to poll from a badge (no side effects).")]
        public async Task<IActionResult> GetUnreadCountAsync()
        {
            var query = new GetUnreadCountQuery { UserId = User.GetUserId()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpPost("mark-all-read")]
        [OpenApiOperation("Mark All Read", "Flips every unread notification for the current user to read. Returns the number of rows updated.")]
        public async Task<IActionResult> MarkAllReadAsync()
        {
            var command = new MarkAllNotificationsReadCommand { UserId = User.GetUserId()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPost("device-token")]
        [OpenApiOperation("Save Device Token", "Saves the FCM token for the user's device to receive push notifications.")]
        public async Task<IActionResult> SaveDeviceTokenAsync([FromBody] SaveDeviceTokenRequest request)
        {
            var command = new SaveDeviceTokenCommand { Request = request, UserId = User.GetUserId()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpDelete("device-token")]
        [OpenApiOperation("Unregister Device Token", "Marks the FCM token inactive for the current user — called on logout so the prior user no longer gets pushes on this device.")]
        public async Task<IActionResult> DeleteDeviceTokenAsync([FromQuery] string fcmToken)
        {
            var command = new DeleteDeviceTokenCommand { FcmToken = fcmToken, UserId = User.GetUserId()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }
    }
}