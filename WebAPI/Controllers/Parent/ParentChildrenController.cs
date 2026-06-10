using Application.Features.Identity.Commands;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Parent
{
    [Route("api/parent/children")]
    [Authorize(Roles = RoleConstants.Parent)]
    [OpenApiTag("Parent - Children", Description = "Endpoints for parents to link and monitor their children")]
    public class ParentChildrenController : BaseApiController
    {
        [HttpPost("link")]
        [OpenApiOperation("Request Link", "Request to link a student using their phone number.")]
        public async Task<IActionResult> RequestLinkAsync([FromBody] RequestLinkCommand command)
        {
            command.ParentId = User.GetUserId()!;
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpDelete("{childId}/unlink")]
        [OpenApiOperation("Unlink Child", "Removes the link between the parent and the child.")]
        public async Task<IActionResult> UnlinkChildAsync(string childId)
        {
            var response = await Sender.Send(new UnlinkChildCommand
            {
                ParentId = User.GetUserId()!,
                StudentId = childId
            });
            return Ok(response);
        }
    }
}