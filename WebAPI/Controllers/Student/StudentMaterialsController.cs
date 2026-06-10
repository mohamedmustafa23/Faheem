using Application.Features.Materials.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Student
{
    [Route("api/student/materials")]
    [Authorize(Roles = RoleConstants.Student)]
    [OpenApiTag("Student - Materials", Description = "Endpoints for viewing and downloading study materials")]
    public class StudentMaterialsController : BaseApiController
    {
        [HttpGet("group/{groupId}")]
        [OpenApiOperation("Get Group Materials", "Gets all uploaded materials for a specific group.")]
        public async Task<IActionResult> GetGroupMaterialsAsync(Guid groupId)
        {
            var query = new GetGroupMaterialsQuery { GroupId = groupId, UserId = User.GetUserId()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("all")]
        [OpenApiOperation("Get All My Materials", "Returns every material across every group the student is enrolled in, sorted newest first.")]
        public async Task<IActionResult> GetAllMyMaterialsAsync()
        {
            var query = new GetStudentAllMaterialsQuery { StudentId = User.GetUserId()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }
    }
}