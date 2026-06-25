using Application.Features.Identity.Commands;
using Application.Features.Identity.DTOs;
using Application.Features.Identity.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Teacher
{
    [Route("api/teacher/assistants")]
    [Authorize(Roles = RoleConstants.Teacher)]
    [OpenApiTag("Teacher - Assistants", Description = "Endpoints for managing teacher assistant accounts")]
    public class TeacherAssistantsController : BaseApiController
    {
        [HttpGet]
        [OpenApiOperation("List Assistants", "Returns all assistants that belong to the current teacher's workspace.")]
        public async Task<IActionResult> ListAsync()
        {
            var response = await Sender.Send(new GetTeacherAssistantsQuery
            {
                TeacherTenantId = User.GetTenant()!
            });
            return Ok(response);
        }

        [HttpPost]
        [OpenApiOperation("Create Assistant", "Creates a new assistant account scoped to the current teacher's workspace.")]
        public async Task<IActionResult> CreateAsync([FromBody] RegisterAssistantRequest request)
        {
            var command = new RegisterAssistantCommand
            {
                Request         = request,
                TeacherTenantId = User.GetTenant()!
            };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPut("{assistantId}/permissions")]
        [OpenApiOperation("Set Assistant Permissions", "Updates the capability flags the teacher grants this assistant.")]
        public async Task<IActionResult> SetPermissionsAsync([FromRoute] string assistantId, [FromBody] SetAssistantPermissionsCommand command)
        {
            command.AssistantUserId = assistantId;
            command.TeacherTenantId = User.GetTenant()!;
            return Ok(await Sender.Send(command));
        }

        [HttpDelete("{assistantId}")]
        [OpenApiOperation("Remove Assistant",
            "Soft-removes an assistant (deactivates + strips workspace claim). Audit history is preserved.")]
        public async Task<IActionResult> RemoveAsync([FromRoute] string assistantId)
        {
            var command = new RemoveAssistantCommand
            {
                AssistantUserId = assistantId,
                TeacherTenantId = User.GetTenant()!
            };
            var response = await Sender.Send(command);
            return Ok(response);
        }
    }
}
