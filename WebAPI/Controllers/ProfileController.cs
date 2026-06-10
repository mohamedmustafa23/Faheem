using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers
{
    [Route("api/profile")]
    [Authorize]
    [OpenApiTag("Profile", Description = "View and update the current user's profile (all roles)")]
    public class ProfileController : BaseApiController
    {
        private readonly IAuthService _authService;

        public ProfileController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>GET /api/profile — returns the current user's profile info.</summary>
        [HttpGet]
        [OpenApiOperation("Get My Profile", "Returns name, email, phone, and user type for the authenticated user.")]
        public async Task<IActionResult> GetProfileAsync(CancellationToken ct)
        {
            var userId = User.GetUserId()!;
            var profile = await _authService.GetProfileAsync(userId, ct);
            return Ok(profile);
        }

        /// <summary>PUT /api/profile — update firstName, lastName, phoneNumber.</summary>
        [HttpPut]
        [OpenApiOperation("Update My Profile", "Updates the display name and phone number for the authenticated user.")]
        public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileRequest request, CancellationToken ct)
        {
            var userId = User.GetUserId()!;
            var result = await _authService.UpdateProfileAsync(userId, request, ct);
            return Ok(result);
        }

        /// <summary>POST /api/profile/change-password — change password using the current password.</summary>
        [HttpPost("change-password")]
        [OpenApiOperation("Change Password", "Changes the password for the authenticated user. Requires the current password. Revokes all active refresh tokens.")]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            var userId = User.GetUserId()!;
            var result = await _authService.ChangePasswordAsync(userId, request, ct);
            return Ok(result);
        }

        /// <summary>DELETE /api/profile — permanently deletes the authenticated user's account.</summary>
        [HttpDelete]
        [OpenApiOperation(
            "Delete My Account",
            "Permanently deletes the authenticated user, all sessions, and owned data. Teachers must wind down active groups first. Required for app-store compliance.")]
        public async Task<IActionResult> DeleteMyAccountAsync(CancellationToken ct)
        {
            var userId = User.GetUserId()!;
            var result = await _authService.DeleteMyAccountAsync(userId, ct);
            return Ok(result);
        }
    }
}
