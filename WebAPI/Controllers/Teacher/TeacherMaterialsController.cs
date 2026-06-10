using Application.Features.Materials.Commands;
using Application.Features.Materials.DTOs;
using Application.Features.Materials.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Teacher
{
    [Route("api/teacher/materials")]
    [Authorize(Roles = $"{RoleConstants.Teacher},{RoleConstants.Assistant}")]
    [OpenApiTag("Teacher - Materials", Description = "Endpoints for uploading and managing study materials")]
    public class TeacherMaterialsController : BaseApiController
    {
        [HttpPost]
        [ShouldHavePermission(AppAction.Create, AppFeature.Groups)]
        [OpenApiOperation("Upload Material", "Uploads a file to one or multiple groups (Max 20MB).")]
        public async Task<IActionResult> UploadMaterialAsync([FromForm] UploadMaterialRequest request)
        {
            var command = new UploadMaterialCommand { Request = request, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpGet("groups/{groupId}")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Groups)]
        [OpenApiOperation("Get Group Materials", "Gets all materials uploaded by the teacher for a specific group.")]
        public async Task<IActionResult> GetTeacherMaterialsAsync(Guid groupId)
        {
            var query = new GetTeacherMaterialsQuery { GroupId = groupId, TenantId = User.GetTenant()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpDelete("{materialId}")]
        [ShouldHavePermission(AppAction.Delete, AppFeature.Groups)]
        [OpenApiOperation("Delete Material", "Deletes a material from the database and removes the physical file from the server if not used elsewhere.")]
        public async Task<IActionResult> DeleteMaterialAsync(Guid materialId)
        {
            var command = new DeleteMaterialCommand { MaterialId = materialId, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }
    }
}