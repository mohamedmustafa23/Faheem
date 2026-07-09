using Application.Features.LessonReports.Commands;
using Application.Features.LessonReports.DTOs;
using Application.Features.LessonReports.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Teacher
{
    [Route("api/teacher/lesson-reports")]
    [Authorize(Roles = $"{RoleConstants.CenterOwner},{RoleConstants.CenterStaff},{RoleConstants.Teacher},{RoleConstants.Assistant}")]
    [OpenApiTag("Teacher - Lesson Reports", Description = "Per-session lesson summary + per-student feedback. Gated by the Attendance capability (same actors, same post-session flow).")]
    public class TeacherLessonReportsController : BaseApiController
    {
        [HttpGet("occurrences/{occurrenceId}")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Attendance)]
        [OpenApiOperation("Get Lesson Report Editor", "Returns the present students for an occurrence plus any saved report, pre-filled for editing.")]
        public async Task<IActionResult> GetOccurrenceReportAsync(Guid occurrenceId)
        {
            var query = new GetOccurrenceReportQuery { OccurrenceId = occurrenceId, TenantId = User.GetTenant()! };
            return Ok(await Sender.Send(query));
        }

        [HttpPost]
        [ShouldHavePermission(AppAction.Create, AppFeature.Attendance)]
        [OpenApiOperation("Save Lesson Report", "Creates or updates the lesson report for an occurrence (summary + homework + per-student feedback).")]
        public async Task<IActionResult> SaveReportAsync([FromBody] SaveLessonReportRequest request)
        {
            var command = new SaveLessonReportCommand { Request = request, TenantId = User.GetTenant()! };
            return Ok(await Sender.Send(command));
        }
    }
}
