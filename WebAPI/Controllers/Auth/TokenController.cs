using Application.Features.Identity.Tokens.Queries;
using Application.Features.Tokens.Commands;
using Application.Features.Tokens.DTOs;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NSwag.Annotations;

namespace WebAPI.Controllers.Auth
{
    [Route("api/token")]
    [OpenApiTag("Token Management", Description = "Endpoints for Login and Refreshing JWT tokens")]
    public class TokenController : BaseApiController
    {
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("LoginPolicy")] 
        [OpenApiOperation("Login", "Authenticates a user and returns a JWT and Refresh Token.")]
        public async Task<IActionResult> GetTokenAsync([FromBody] TokenRequest request)
        {
            var response = await Sender.Send(new GetTokenQuery { TokenRequest = request });
            return Ok(response);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [OpenApiOperation("Refresh Token", "Generates a new JWT using a valid refresh token.")]
        public async Task<IActionResult> GetRefreshTokenAsync([FromBody] RefreshTokenRequest request)
        {
            var response = await Sender.Send(new GetRefreshTokenQuery { RefreshTokenRequest = request });
            return Ok(response);
        }

        [HttpPost("select-workspace")]
        [Authorize]
        [OpenApiOperation("Select Workspace", "Exchanges the account token for a full access token scoped to the chosen workspace (also used to switch workspace).")]
        public async Task<IActionResult> SelectWorkspaceAsync([FromBody] SelectWorkspaceRequest request)
        {
            var command = new SelectWorkspaceCommand
            {
                UserId = ClaimsPrincipalExtensions.GetUserId(User)!,
                TenantId = request.TenantId
            };

            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        [OpenApiOperation("Logout", "Revokes the refresh token and logs the user out from the current device.")]
        public async Task<IActionResult> LogoutAsync([FromBody] LogoutCommand command) 
        {
            command.UserId = Infrastructure.Identity.ClaimsPrincipalExtensions.GetUserId(User)!;

            var response = await Sender.Send(command);
            return Ok(response);
        }
    }
}