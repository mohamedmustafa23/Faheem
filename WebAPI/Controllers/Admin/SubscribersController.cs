using Application.Features.Tenancy.Commands;
using Application.Features.Tenancy.DTOs;
using Application.Features.Tenancy.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Admin
{
    [Route("api/admin/subscribers")]
    [Authorize(Roles = RoleConstants.Admin)]
    [OpenApiTag("Admin - Subscribers", Description = "The admin control center: subscribers with live data + subscription management.")]
    public class SubscribersController : BaseApiController
    {
        [HttpGet]
        [ShouldHavePermission(AppAction.Read, AppFeature.Tenants)]
        [OpenApiOperation("Get Subscribers", "Every subscriber (teacher workspace or center) with subscription status, owner contact, and live counts. Soonest-to-expire first.")]
        public async Task<IActionResult> GetAsync()
            => Ok(await Sender.Send(new GetSubscribersQuery()));

        [HttpGet("{id}")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Tenants)]
        [OpenApiOperation("Get Subscriber", "One subscriber's full control-center detail.")]
        public async Task<IActionResult> GetByIdAsync(string id)
            => Ok(await Sender.Send(new GetSubscriberByIdQuery { Id = id }));

        [HttpPost("{id}/extend")]
        [ShouldHavePermission(AppAction.UpgradeSubscription, AppFeature.Tenants)]
        [OpenApiOperation("Extend Subscription", "Extends the subscription by N months (from today if expired) and reactivates it.")]
        public async Task<IActionResult> ExtendAsync(string id, [FromBody] ExtendSubscriptionRequest request)
            => Ok(await Sender.Send(new ExtendSubscriptionCommand { Id = id, Months = request.Months, MaxTeachers = request.MaxTeachers }));

        [HttpPut("{id}/active")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Tenants)]
        [OpenApiOperation("Set Subscriber Active", "Activates or suspends a subscriber.")]
        public async Task<IActionResult> SetActiveAsync(string id, [FromBody] SetSubscriberActiveRequest request)
            => Ok(await Sender.Send(new SetSubscriberActiveCommand { Id = id, IsActive = request.IsActive }));

        [HttpPut("{id}/seats")]
        [ShouldHavePermission(AppAction.UpgradeSubscription, AppFeature.Tenants)]
        [OpenApiOperation("Set Center Seats", "Sets a center's teacher seat limit (null = unlimited). Centers only.")]
        public async Task<IActionResult> SetSeatsAsync(string id, [FromBody] SetCenterSeatsRequest request)
            => Ok(await Sender.Send(new SetCenterSeatsCommand { Id = id, MaxTeachers = request.MaxTeachers }));

        [HttpDelete("{id}")]
        [OpenApiOperation("Delete Subscriber", "Removes a subscriber's workspace (must have no groups). Clears churned / orphaned accounts.")]
        public async Task<IActionResult> DeleteAsync(string id)
            => Ok(await Sender.Send(new DeleteSubscriberCommand { Id = id }));
    }
}
