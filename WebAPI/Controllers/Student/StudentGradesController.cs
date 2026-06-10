using Application.Features.Grades.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Student
{
    [Route("api/student/grades")]
    [Authorize(Roles = RoleConstants.Student)]
    [OpenApiTag("Student - Grades", Description = "Endpoints for students to view their exam results")]
    public class StudentGradesController : BaseApiController
    {
        [HttpGet]
        [OpenApiOperation("Get My Grades", "Retrieves all grades for the logged-in student across all teachers.")]
        public async Task<IActionResult> GetMyGradesAsync()
        {
            var query = new GetStudentGradesQuery { StudentId = User.GetUserId()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }
    }
}