using Application.Features.Payments.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Student
{
    [Route("api/student/payments")]
    [Authorize(Roles = RoleConstants.Student)]
    [OpenApiTag("Student - Payments", Description = "Endpoints for the student to view their own financial obligations")]
    public class StudentPaymentsController : BaseApiController
    {
        [HttpGet("my")]
        [OpenApiOperation(
            "Get My Payments",
            "Returns the student's full payment picture: totals across all groups, plus per-group breakdown (open cycle record + standalone occurrences) with transaction history. Waived records contribute 0 to outstanding totals.")]
        public async Task<IActionResult> GetMyPaymentsAsync()
        {
            var query = new GetMyPaymentsQuery { StudentId = User.GetUserId()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }
    }
}
