using Application.Features.Centers.Commands;
using Application.Features.Centers.DTOs;
using Infrastructure.Constants;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Admin
{
    [Route("api/admin/centers")]
    [Authorize(Roles = RoleConstants.Admin)]
    [OpenApiTag("Admin - Centers", Description = "System administration endpoints for creating tutoring centers")]
    public class CentersController : BaseApiController
    {
        // Center creation is self-service (POST /api/auth/register/center). The admin's only
        // job here is activating / renewing the subscription once the center owner exists.

        [HttpPut("subscription")]
        [ShouldHavePermission(AppAction.UpgradeSubscription, AppFeature.Tenants)]
        [OpenApiOperation("Set Center Subscription", "Activates or renews a center subscription: sets the teacher seat limit and extends the valid-until date (unused days are preserved on early renewal).")]
        public async Task<IActionResult> SetSubscriptionAsync([FromBody] SetCenterSubscriptionRequest request)
        {
            var response = await Sender.Send(new SetCenterSubscriptionCommand { Request = request });
            return Ok(response);
        }
    }
}
