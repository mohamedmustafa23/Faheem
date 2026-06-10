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
    [Route("api/admin/tenants")]
    [Authorize(Roles = RoleConstants.Admin)]
    [OpenApiTag("Admin - Tenants", Description = "System administration endpoints for managing teacher workspaces (Tenants)")]
    public class TenantsController : BaseApiController
    {
        [HttpPost]
        [ShouldHavePermission(AppAction.Create, AppFeature.Tenants)]
        [OpenApiOperation("Create Tenant", "Creates a new tenant (Teacher Workspace) manually.")]
        public async Task<IActionResult> CreateTenantAsync([FromBody] CreateTenantRequest request)
        {
            var response = await Sender.Send(new CreateTenantCommand { CreateTenantRequest = request });
            return Ok(response);
        }

        [HttpPut("{id}/activate")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Tenants)]
        [OpenApiOperation("Activate Tenant", "Activates a suspended tenant subscription.")]
        public async Task<IActionResult> ActivateTenantAsync(string id)
        {
            var response = await Sender.Send(new ActivateTenantCommand { TenantId = id });
            return Ok(response);
        }

        [HttpPut("{id}/deactivate")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Tenants)]
        [OpenApiOperation("Deactivate Tenant", "Suspends a tenant, preventing write operations.")]
        public async Task<IActionResult> DeactivateTenantAsync(string id)
        {
            var response = await Sender.Send(new DeactivateTenantCommand { TenantId = id });
            return Ok(response);
        }

        [HttpPut("subscription")]
        [ShouldHavePermission(AppAction.UpgradeSubscription, AppFeature.Tenants)]
        [OpenApiOperation("Update Subscription", "Upgrades or extends a tenant's subscription valid date.")]
        public async Task<IActionResult> UpdateSubscriptionAsync([FromBody] UpdateTenantSubscriptionRequest request)
        {
            var response = await Sender.Send(new UpdateTenantSubscriptionCommand { Request = request });
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Tenants)]
        [OpenApiOperation("Get Tenant Details", "Gets full details of a specific tenant by ID.")]
        public async Task<IActionResult> GetTenantByIdAsync(string id)
        {
            var response = await Sender.Send(new GetTenantByIdQuery { TenantId = id });
            return Ok(response);
        }

        [HttpGet]
        [ShouldHavePermission(AppAction.Read, AppFeature.Tenants)]
        [OpenApiOperation("Get All Tenants", "Retrieves a list of all registered tenants in the system.")]
        public async Task<IActionResult> GetTenantsAsync()
        {
            var response = await Sender.Send(new GetTenantsQuery());
            return Ok(response);
        }
    }
}