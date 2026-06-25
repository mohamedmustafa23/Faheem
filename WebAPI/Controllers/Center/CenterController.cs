using Application.Features.Centers.Commands;
using Application.Features.Centers.DTOs;
using Application.Features.Centers.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Center
{
    [Route("api/center")]
    [Authorize(Roles = $"{RoleConstants.CenterOwner},{RoleConstants.Teacher},{RoleConstants.Assistant}")]
    [OpenApiTag("Center", Description = "Center owner manages members + subscription; invited teachers respond to invites")]
    public class CenterController : BaseApiController
    {
        // ── Owner operations (scoped to the currently-selected center workspace) ──

        [HttpGet("overview")]
        [OpenApiOperation("Get Center Overview", "Dashboard summary for the current center: name, subscription, seats, member counts (owner only).")]
        public async Task<IActionResult> GetOverviewAsync()
        {
            var query = new GetCenterOverviewQuery
            {
                TenantId = User.GetTenant()!,
                OwnerUserId = User.GetUserId()!
            };
            return Ok(await Sender.Send(query));
        }

        [HttpPost("invite")]
        [OpenApiOperation("Invite Teacher", "Center owner invites an existing user to join the center as a teacher.")]
        public async Task<IActionResult> InviteTeacherAsync([FromBody] InviteTeacherRequest request)
        {
            var command = new InviteTeacherCommand
            {
                TenantId = User.GetTenant()!,
                OwnerUserId = User.GetUserId()!,
                Request = request
            };
            return Ok(await Sender.Send(command));
        }

        [HttpGet("members")]
        [OpenApiOperation("Get Center Members", "Lists every member and pending invite of the current center (owner only).")]
        public async Task<IActionResult> GetMembersAsync()
        {
            var query = new GetCenterMembersQuery
            {
                TenantId = User.GetTenant()!,
                OwnerUserId = User.GetUserId()!
            };
            return Ok(await Sender.Send(query));
        }

        [HttpDelete("members/{memberUserId}")]
        [OpenApiOperation("Remove Center Member", "Removes a member (or pending invite) from the current center (owner only).")]
        public async Task<IActionResult> RemoveMemberAsync(string memberUserId)
        {
            var command = new RemoveCenterMemberCommand
            {
                TenantId = User.GetTenant()!,
                OwnerUserId = User.GetUserId()!,
                MemberUserId = memberUserId
            };
            return Ok(await Sender.Send(command));
        }

        [HttpPut("members/{memberUserId}/share")]
        [OpenApiOperation("Set Teacher Share", "Sets the center's revenue cut (%) for a teacher member (owner only).")]
        public async Task<IActionResult> SetTeacherShareAsync(string memberUserId, [FromBody] SetTeacherShareRequest request)
        {
            var command = new SetTeacherShareCommand
            {
                TenantId = User.GetTenant()!,
                OwnerUserId = User.GetUserId()!,
                TeacherUserId = memberUserId,
                Request = request
            };
            return Ok(await Sender.Send(command));
        }

        [HttpGet("financials")]
        [OpenApiOperation("Get Center Financials", "Per-teacher revenue report (collected, center cut, teacher cut) + grand totals (owner only).")]
        public async Task<IActionResult> GetFinancialsAsync()
        {
            var query = new GetCenterFinancialsQuery
            {
                TenantId = User.GetTenant()!,
                OwnerUserId = User.GetUserId()!
            };
            return Ok(await Sender.Send(query));
        }

        [HttpGet("financials/{teacherId}")]
        [OpenApiOperation("Get Teacher Financial Detail", "One teacher's totals + per-group breakdown for the drill-in screen and statement (owner only).")]
        public async Task<IActionResult> GetTeacherFinancialDetailAsync(string teacherId)
        {
            var query = new GetCenterTeacherDetailQuery
            {
                TenantId = User.GetTenant()!,
                OwnerUserId = User.GetUserId()!,
                TeacherUserId = teacherId
            };
            return Ok(await Sender.Send(query));
        }

        // ── Invited-user operations (independent of the selected workspace) ──

        [HttpGet("invites")]
        [OpenApiOperation("Get My Invites", "Lists the center invites awaiting the current user's response.")]
        public async Task<IActionResult> GetMyInvitesAsync()
        {
            var query = new GetMyInvitesQuery { UserId = User.GetUserId()! };
            return Ok(await Sender.Send(query));
        }

        [HttpPost("invites/respond")]
        [OpenApiOperation("Respond To Invite", "Accept or decline a pending center invite.")]
        public async Task<IActionResult> RespondToInviteAsync([FromBody] RespondToInviteCommand command)
        {
            command.UserId = User.GetUserId()!;
            return Ok(await Sender.Send(command));
        }
    }
}
