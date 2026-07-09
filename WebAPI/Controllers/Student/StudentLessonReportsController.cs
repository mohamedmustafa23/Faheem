using Application.Features.LessonReports.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Student
{
    [Route("api/student/lesson-reports")]
    [Authorize(Roles = RoleConstants.Student)]
    [OpenApiTag("Student - Lesson Reports", Description = "Endpoints for students to view their own lesson summaries + feedback")]
    public class StudentLessonReportsController : BaseApiController
    {
        [HttpGet("groups/{groupId}")]
        [OpenApiOperation("Get My Lesson Reports", "My per-session lesson summaries + feedback in one group, newest first.")]
        public async Task<IActionResult> GetMyGroupReportsAsync(Guid groupId)
        {
            var query = new GetStudentGroupReportsQuery { StudentId = User.GetUserId()!, GroupId = groupId };
            return Ok(await Sender.Send(query));
        }
    }
}
