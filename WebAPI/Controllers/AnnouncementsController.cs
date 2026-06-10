using Application.Features.Announcements.Commands;
using Application.Features.Announcements.DTOs;
using Application.Features.Announcements.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [Route("api/announcements")]
    [Authorize]
    [OpenApiTag("Shared - Announcements", Description = "Endpoints for group announcements (Teacher Feed)")]
    public class AnnouncementsController : BaseApiController
    {
        [HttpGet("groups/{groupId}")]
        [OpenApiOperation("Get Announcements", "Gets the top 50 announcements for a group. Pinned messages appear first.")]
        public async Task<IActionResult> GetAnnouncementsAsync(Guid groupId)
        {
            var query = new GetGroupAnnouncementsQuery
            {
                GroupId = groupId,
                UserId = User.GetUserId()!,
                UserRole = User.FindFirstValue(ClaimTypes.Role) ?? ""
            };

            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = RoleConstants.Teacher)]
        [OpenApiOperation("Create Announcement", "Teacher posts an announcement to one or multiple groups.")]
        public async Task<IActionResult> CreateAnnouncementAsync([FromBody] CreateAnnouncementRequest request)
        {
            var command = new CreateAnnouncementCommand
            {
                Request = request,
                TenantId = User.GetTenant()!
            };

            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpDelete("{announcementId}")]
        [Authorize(Roles = RoleConstants.Teacher)]
        [OpenApiOperation("Delete Announcement", "Teacher deletes an announcement.")]
        public async Task<IActionResult> DeleteAnnouncementAsync(Guid announcementId)
        {
            var command = new DeleteAnnouncementCommand
            {
                AnnouncementId = announcementId,
                TenantId = User.GetTenant()!
            };

            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPut("{announcementId}/toggle-pin")]
        [Authorize(Roles = RoleConstants.Teacher)]
        [OpenApiOperation("Toggle Pin", "Teacher pins or unpins an announcement.")]
        public async Task<IActionResult> TogglePinAsync(Guid announcementId)
        {
            var command = new TogglePinCommand
            {
                AnnouncementId = announcementId,
                TenantId = User.GetTenant()!
            };

            var response = await Sender.Send(command);
            return Ok(response);
        }
    }
}