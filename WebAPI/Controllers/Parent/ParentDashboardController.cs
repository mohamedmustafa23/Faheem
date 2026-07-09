using Application.Features.LessonReports.Queries;
using Application.Features.Parents.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Parent
{
    [Route("api/parent/dashboard")]
    [Authorize(Roles = RoleConstants.Parent)]
    [OpenApiTag("Parent - Dashboard", Description = "Endpoints for parent's main screens")]
    public class ParentDashboardController : BaseApiController
    {
        [HttpGet("children")]
        [OpenApiOperation("Get My Children", "Gets a list of all successfully linked children.")]
        public async Task<IActionResult> GetMyChildrenAsync()
        {
            var response = await Sender.Send(new GetMyChildrenQuery { ParentId = User.GetUserId()! });
            return Ok(response);
        }

        [HttpGet("children/{childId}/details")]
        [OpenApiOperation("Get Child Details", "Gets the groups and today's session occurrences for a specific child. Provide date in YYYY-MM-DD format; defaults to today (UTC).")]
        public async Task<IActionResult> GetChildDetailsAsync(string childId, [FromQuery] DateOnly? today)
        {
            var response = await Sender.Send(new GetChildDetailsQuery
            {
                ParentId = User.GetUserId()!,
                ChildId = childId,
                Today = today
            });
            return Ok(response);
        }

        [HttpGet("children/{childId}/groups")]
        [OpenApiOperation("Get Child Groups Overview", "Group-centric snapshot for a child: per group rank, attendance, grades average, and fees — powers the parent's group-by-group child screen.")]
        public async Task<IActionResult> GetChildGroupsOverviewAsync(string childId)
        {
            var query = new GetChildGroupsOverviewQuery { ParentId = User.GetUserId()!, ChildId = childId };
            return Ok(await Sender.Send(query));
        }

        [HttpGet("children/{childId}/groups/{groupId}/reports")]
        [OpenApiOperation("Get Child Lesson Reports", "The child's per-session lesson reports in one group (summary + homework + this child's feedback), newest first.")]
        public async Task<IActionResult> GetChildGroupReportsAsync(string childId, Guid groupId)
        {
            var query = new GetChildGroupReportsQuery { ParentId = User.GetUserId()!, ChildId = childId, GroupId = groupId };
            return Ok(await Sender.Send(query));
        }

        [HttpGet("children/{childId}/grades")]
        [OpenApiOperation("Get Child Grades", "Gets all grades for a specific linked child.")]
        public async Task<IActionResult> GetChildGradesAsync(string childId)
        {
            var query = new GetChildGradesQuery { ParentId = User.GetUserId()!, ChildId = childId };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("children/{childId}/overview")]
        [OpenApiOperation(
            "Get Child Overview",
            "Single round-trip snapshot for the parent's dashboard card / child Overview tab. Combines attendance, grades, payments, and today's schedule.")]
        public async Task<IActionResult> GetChildOverviewAsync(string childId, [FromQuery] DateOnly? today)
        {
            var query = new GetChildOverviewQuery { ParentId = User.GetUserId()!, ChildId = childId, Today = today };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("children/{childId}/attendance")]
        [OpenApiOperation("Get Child Attendance Summary", "Per-group attendance summary for the child (present/absent/excused + rate).")]
        public async Task<IActionResult> GetChildAttendanceAsync(string childId)
        {
            var query = new GetChildAttendanceSummaryQuery { ParentId = User.GetUserId()!, ChildId = childId };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("children/{childId}/attendance/groups/{groupId}")]
        [OpenApiOperation("Get Child Group Attendance Detail", "Full attendance history for the child in one group — drives the calendar view.")]
        public async Task<IActionResult> GetChildGroupAttendanceAsync(string childId, Guid groupId)
        {
            var query = new GetChildGroupAttendanceDetailQuery { ParentId = User.GetUserId()!, ChildId = childId, GroupId = groupId };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("children/{childId}/absences")]
        [OpenApiOperation(
            "Get Child Absences",
            "Recent absences (Absent or Excused) for the child across every group, newest first. Each row carries date, time, group, status, and the teacher's excuse note. Pass take=N to override the default 30-row page.")]
        public async Task<IActionResult> GetChildAbsencesAsync(string childId, [FromQuery] int take = 30)
        {
            var query = new GetChildAbsencesQuery { ParentId = User.GetUserId()!, ChildId = childId, Take = take };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("children/{childId}/payments")]
        [OpenApiOperation("Get Child Payments", "Payments overview for the child across every group, same shape as the student's own ماليتي screen.")]
        public async Task<IActionResult> GetChildPaymentsAsync(string childId)
        {
            var query = new GetChildPaymentsQuery { ParentId = User.GetUserId()!, ChildId = childId };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("children/{childId}/announcements")]
        [OpenApiOperation("Get Child Announcements", "Announcements across every group the child is in, newest first, each tagged with its group + teacher.")]
        public async Task<IActionResult> GetChildAnnouncementsAsync(string childId)
        {
            var query = new GetChildAnnouncementsQuery { ParentId = User.GetUserId()!, ChildId = childId };
            var response = await Sender.Send(query);
            return Ok(response);
        }
    }
}
