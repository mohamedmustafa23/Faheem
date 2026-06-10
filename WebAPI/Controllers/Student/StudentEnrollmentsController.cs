using Application.Features.Enrollment.Commands;
using Application.Features.Groups.DTOs;
using Application.Features.Identity.Commands;
using Application.Features.Students.Commands;
using Application.Features.Students.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Student
{
    [Route("api/student/enrollments")]
    [Authorize(Roles = RoleConstants.Student)]
    [OpenApiTag("Student - Enrollments", Description = "Endpoints for students to join groups and manage parent links")]
    public class StudentEnrollmentsController : BaseApiController
    {
        [HttpPost("join")]
        [ShouldHavePermission(AppAction.Create, AppFeature.Enrollment)]
        [OpenApiOperation("Join Group", "Student joins a group using the 6-character enrollment code.")]
        public async Task<IActionResult> JoinGroupAsync([FromBody] JoinGroupRequest request) // 👈 بنستقبل الـ Request
        {
            var command = new JoinGroupCommand
            {
                EnrollmentCode = request.EnrollmentCode,
                StudentId = User.GetUserId()!
            };

            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPut("parent-link/respond")]
        [OpenApiOperation("Respond to Parent Link", "Accept or Reject a parent's link request.")]
        public async Task<IActionResult> RespondToLinkAsync([FromBody] RespondToLinkCommand command)
        {
            command.StudentId = User.GetUserId()!;
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpDelete("groups/{groupId}/leave")]
        [OpenApiOperation("Leave Group", "Student leaves a group they are enrolled in.")]
        public async Task<IActionResult> LeaveGroupAsync(Guid groupId)
        {
            var response = await Sender.Send(new LeaveGroupCommand
            {
                GroupId = groupId,
                StudentId = User.GetUserId()!
            });
            return Ok(response);
        }

        [HttpGet("parents")]
        [OpenApiOperation("Get My Linked Parents", "Lists the accepted parent links for the current student — drives the 'أهلي' panel.")]
        public async Task<IActionResult> GetMyLinkedParentsAsync()
        {
            var query = new GetMyLinkedParentsQuery { StudentId = User.GetUserId()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpDelete("parents/{parentId}/unlink")]
        [OpenApiOperation("Unlink Parent", "Student-initiated unlink. Removes the parent-student row so the parent can no longer see this student's data.")]
        public async Task<IActionResult> UnlinkParentAsync(string parentId)
        {
            var command = new UnlinkParentCommand { StudentId = User.GetUserId()!, ParentId = parentId };
            var response = await Sender.Send(command);
            return Ok(response);
        }
    }
}