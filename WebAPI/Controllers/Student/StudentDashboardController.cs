using Application.Features.Students.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Student
{
    [Route("api/student/dashboard")]
    [Authorize(Roles = RoleConstants.Student)]
    [OpenApiTag("Student - Dashboard", Description = "Endpoints for student's main screens")]
    public class StudentDashboardController : BaseApiController
    {
        [HttpGet("groups")]
        [OpenApiOperation("Get My Groups", "Gets all groups the student is enrolled in.")]
        public async Task<IActionResult> GetMyGroupsAsync()
        {
            var response = await Sender.Send(new GetStudentGroupsQuery { StudentId = User.GetUserId()! });
            return Ok(response);
        }

        [HttpGet("schedule/today")]
        [OpenApiOperation("Get Today's Schedule", "Gets the student's session occurrences for a specific date (YYYY-MM-DD). Defaults to today (UTC) if not provided.")]
        public async Task<IActionResult> GetTodayScheduleAsync([FromQuery] DateOnly? today)
        {
            var response = await Sender.Send(new GetStudentTodayScheduleQuery
            {
                StudentId = User.GetUserId()!,
                Today = today
            });
            return Ok(response);
        }

        [HttpGet("parent-requests/pending")]
        [OpenApiOperation("Get Pending Parent Requests", "Gets all pending link requests from parents.")]
        public async Task<IActionResult> GetPendingRequestsAsync()
        {
            var response = await Sender.Send(new GetPendingParentRequestsQuery { StudentId = User.GetUserId()! });
            return Ok(response);
        }
    }
}
