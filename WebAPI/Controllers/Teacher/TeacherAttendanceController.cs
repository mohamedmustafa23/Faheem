using Application.Features.Attendance.Commands;
using Application.Features.Attendance.DTOs;
using Application.Features.Attendance.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Teacher
{
    [Route("api/teacher/attendance")]
    [Authorize(Roles = $"{RoleConstants.CenterOwner},{RoleConstants.Teacher},{RoleConstants.Assistant}")]
    [OpenApiTag("Teacher - Attendance", Description = "Endpoints for managing manual and QR attendance")]
    public class TeacherAttendanceController : BaseApiController
    {
        [HttpGet("occurrences/{occurrenceId}")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Attendance)]
        [OpenApiOperation("Get Occurrence Attendance", "Gets the attendance list for a session occurrence. Defaults to Absent for unmarked students.")]
        public async Task<IActionResult> GetOccurrenceAttendanceAsync(Guid occurrenceId)
        {
            var query = new GetSessionAttendanceQuery { OccurrenceId = occurrenceId, TenantId = User.GetTenant()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("groups/{groupId}/summary")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Attendance)]
        [OpenApiOperation("Get Group Attendance Summary", "Returns each student's total attended/absent/excused count and attendance rate across all completed sessions in a group.")]
        public async Task<IActionResult> GetGroupAttendanceSummaryAsync(Guid groupId)
        {
            var query = new GetGroupAttendanceSummaryQuery { GroupId = groupId, TenantId = User.GetTenant()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("groups/{groupId}/occurrences")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Attendance)]
        [OpenApiOperation("Get Group Occurrences", "Returns a paginated list of all occurrences for a group (completed, cancelled, scheduled) with attendance counts per occurrence.")]
        public async Task<IActionResult> GetGroupOccurrencesAsync(Guid groupId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = new GetGroupOccurrencesQuery { GroupId = groupId, Page = page, PageSize = pageSize, TenantId = User.GetTenant()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpPost("occurrences/save")]
        [ShouldHavePermission(AppAction.Create, AppFeature.Attendance)]
        [OpenApiOperation("Save Attendance", "Saves attendance records for a session occurrence. Does NOT close the session — use End Session for that.")]
        public async Task<IActionResult> SaveAttendanceAsync([FromBody] SaveAttendanceRequest request)
        {
            var command = new SaveAttendanceCommand { Request = request, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPut("occurrences/{occurrenceId}/end")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Attendance)]
        [OpenApiOperation("End Session", "Marks the occurrence as Completed, auto-marks absent students, schedules the next occurrence, and checks cycle completion.")]
        public async Task<IActionResult> EndSessionAsync(Guid occurrenceId)
        {
            var command = new EndSessionCommand { OccurrenceId = occurrenceId, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPost("scan")]
        [ShouldHavePermission(AppAction.Create, AppFeature.Attendance)]
        [OpenApiOperation("Scan Student Code", "Reads a student's signed check-in code and marks them present in the session it names — one camera, no session switching. Used by both individual teachers and centers.")]
        public async Task<IActionResult> ScanStudentCodeAsync([FromBody] CenterScanRequest request)
        {
            var command = new CenterScanCommand { TenantId = User.GetTenant()!, Request = request };
            return Ok(await Sender.Send(command));
        }
    }
}
