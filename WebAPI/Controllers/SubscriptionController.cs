using Application.Features.Tenancy.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers
{
    [Route("api/subscription")]
    [Authorize(Roles = $"{RoleConstants.CenterOwner},{RoleConstants.Teacher}")]
    [OpenApiTag("Subscription", Description = "The signed-in owner's subscription status (drives the in-app renewal banner).")]
    public class SubscriptionController : BaseApiController
    {
        [HttpGet("me")]
        [OpenApiOperation("Get My Subscription", "The current workspace's subscription status — days left, expiry, and state.")]
        public async Task<IActionResult> GetMineAsync()
            => Ok(await Sender.Send(new GetMySubscriptionQuery { TenantId = User.GetTenant() ?? string.Empty }));
    }
}
