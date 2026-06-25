using Application.Features.Groups.Commands;
using Application.Features.Groups.DTOs;
using Application.Features.Groups.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Teacher
{
    [Route("api/teacher/groups")]
    [Authorize(Roles = $"{RoleConstants.CenterOwner},{RoleConstants.CenterStaff},{RoleConstants.Teacher},{RoleConstants.Assistant}")]
    [OpenApiTag("Teacher - Groups", Description = "Endpoints for teachers to manage their study groups")]
    public class TeacherGroupsController : BaseApiController
    {
        [HttpPost]
        [ShouldHavePermission(AppAction.Create, AppFeature.Groups)]
        [OpenApiOperation("Create Group", "Create a new study group and generate a unique enrollment code.")]
        public async Task<IActionResult> CreateGroupAsync([FromBody] CreateGroupRequest request)
        {
            var command = new CreateGroupCommand { Request = request, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpGet]
        [ShouldHavePermission(AppAction.Read, AppFeature.Groups)]
        [OpenApiOperation("Get Groups", "Get all study groups for the current teacher.")]
        public async Task<IActionResult> GetTeacherGroupsAsync()
        {
            var query = new GetTeacherGroupsQuery { TenantId = User.GetTenant()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("{id}/details")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Groups)]
        [OpenApiOperation("Get Group Details", "Gets the full details of a group including its sessions and enrolled students.")]
        public async Task<IActionResult> GetGroupDetailsAsync(Guid id)
        {
            var query = new GetGroupDetailsQuery { GroupId = id, TenantId = User.GetTenant()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpPut]
        [ShouldHavePermission(AppAction.Update, AppFeature.Groups)]
        [OpenApiOperation("Update Group", "Updates the details of an existing group.")]
        public async Task<IActionResult> UpdateGroupAsync([FromBody] UpdateGroupRequest request)
        {
            var command = new UpdateGroupCommand { Request = request, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [ShouldHavePermission(AppAction.Delete, AppFeature.Groups)]
        [OpenApiOperation("Delete Group", "Permanently deletes a group and all related data (sessions, attendance, payments, exams, materials, announcements).")]
        public async Task<IActionResult> DeleteGroupAsync(Guid id)
        {
            var command = new DeleteGroupCommand { GroupId = id, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPut("{id}/regenerate-code")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Groups)]
        [OpenApiOperation("Regenerate Code", "Generates a new enrollment code for the group. The old code will no longer work.")]
        public async Task<IActionResult> RegenerateCodeAsync(Guid id)
        {
            var response = await Sender.Send(new RegenerateGroupCodeCommand { GroupId = id, TenantId = User.GetTenant()! });
            return Ok(response);
        }

        [HttpPost("{id}/toggle-pin")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Groups)]
        [OpenApiOperation("Toggle Pin", "Toggles whether this group is pinned to the top of the teacher's list.")]
        public async Task<IActionResult> TogglePinAsync(Guid id)
        {
            var response = await Sender.Send(new ToggleGroupPinCommand { GroupId = id, TenantId = User.GetTenant()! });
            return Ok(response);
        }

        [HttpDelete("{groupId}/students/{studentId}")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Groups)]
        [OpenApiOperation("Remove Student", "Removes a student from the group.")]
        public async Task<IActionResult> RemoveStudentAsync(Guid groupId, string studentId)
        {
            var response = await Sender.Send(new RemoveStudentCommand
            {
                GroupId = groupId,
                StudentId = studentId,
                TenantId = User.GetTenant()!
            });
            return Ok(response);
        }

        [HttpPost("{groupId}/students/manual-add")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Groups)]
        [OpenApiOperation("Manual Add Student", "Manually adds a student to the group. Creates a ghost account if they don't exist.")]
        public async Task<IActionResult> ManualAddStudentAsync(Guid groupId, [FromBody] ManualAddStudentBodyRequest request)
        {
            var command = new ManualAddStudentCommand
            {
                GroupId = groupId,
                TenantId = User.GetTenant()!,
                Request = request
            };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPost("{groupId}/students/add-by-code")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Groups)]
        [OpenApiOperation("Add Student By Code", "Adds an existing manually-added (ghost) student to the group using their student code.")]
        public async Task<IActionResult> AddStudentByCodeAsync(Guid groupId, [FromBody] AddStudentByCodeCommand command)
        {
            command.GroupId = groupId;
            command.TenantId = User.GetTenant()!;
            return Ok(await Sender.Send(command));
        }

        [HttpPut("{groupId}/students/{studentId}")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Groups)]
        [OpenApiOperation("Edit Manual Student", "Edits a manually-added (ghost) student's info and optionally links a parent.")]
        public async Task<IActionResult> EditStudentAsync(Guid groupId, string studentId, [FromBody] EditStudentRequest request)
        {
            var command = new EditStudentCommand
            {
                GroupId = groupId,
                StudentId = studentId,
                TenantId = User.GetTenant()!,
                Request = request
            };
            return Ok(await Sender.Send(command));
        }
    }
}
