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
        [HttpPost]
        [ShouldHavePermission(AppAction.Create, AppFeature.Tenants)]
        [OpenApiOperation("Create Center", "Creates a Center workspace owned by an existing user, with an optional teacher seat limit.")]
        public async Task<IActionResult> CreateCenterAsync([FromBody] CreateCenterRequest request)
        {
            var response = await Sender.Send(new CreateCenterCommand { Request = request });
            return Ok(response);
        }
    }
}
