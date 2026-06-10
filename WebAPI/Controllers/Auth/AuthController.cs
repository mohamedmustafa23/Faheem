using Application.Features.Identity.Commands;
using Application.Features.Identity.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NSwag.Annotations;

namespace WebAPI.Controllers.Auth
{
    [Route("api/auth")] 
    [OpenApiTag("Authentication", Description = "Endpoints for user registration, OTP, and password management")]
    public class AuthController : BaseApiController
    {
        [HttpPost("register/teacher")]
        [AllowAnonymous]
        [EnableRateLimiting("RegisterPolicy")]
        [OpenApiOperation("Register Teacher", "Registers a new teacher and automatically creates a new workspace/tenant.")]
        public async Task<IActionResult> RegisterTeacherAsync([FromBody] RegisterTeacherRequest request)
        {
            var response = await Sender.Send(new RegisterTeacherCommand { Request = request });
            return Ok(response);
        }

        [HttpPost("register/student")]
        [AllowAnonymous]
        [EnableRateLimiting("RegisterPolicy")]
        [OpenApiOperation("Register Student", "Registers a new student globally on the platform.")]
        public async Task<IActionResult> RegisterStudentAsync([FromBody] RegisterStudentRequest request)
        {
            var response = await Sender.Send(new RegisterStudentCommand { Request = request });
            return Ok(response);
        }

        [HttpPost("register/parent")]
        [AllowAnonymous]
        [EnableRateLimiting("RegisterPolicy")]
        [OpenApiOperation("Register Parent", "Registers a new parent globally on the platform.")]
        public async Task<IActionResult> RegisterParentAsync([FromBody] RegisterParentRequest request)
        {
            var response = await Sender.Send(new RegisterParentCommand { Request = request });
            return Ok(response);
        }

        [HttpPost("send-otp")]
        [AllowAnonymous]
        [OpenApiOperation("Send OTP", "Sends a 6-digit OTP to the user's email for verification.")]
        public async Task<IActionResult> SendOtpAsync([FromBody] SendOtpRequest request)
        {
            var response = await Sender.Send(new SendOtpCommand { Request = request });
            return Ok(response);
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        [EnableRateLimiting("OtpPolicy")] 
        [OpenApiOperation("Verify OTP", "Verifies the OTP code sent to the email to activate the account.")]
        public async Task<IActionResult> VerifyOtpAsync([FromBody] VerifyOtpRequest request)
        {
            var response = await Sender.Send(new VerifyOtpCommand { Request = request });
            return Ok(response);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [OpenApiOperation("Forgot Password", "Requests a password reset code to be sent to email.")]
        public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request)
        {
            var response = await Sender.Send(new ForgotPasswordCommand { Request = request });
            return Ok(response);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("OtpPolicy")] 
        [OpenApiOperation("Reset Password", "Resets the user's password using the OTP code.")]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request)
        {
            var response = await Sender.Send(new ResetPasswordCommand { Request = request });
            return Ok(response);
        }

        [HttpPost("register/assistant")]
        [Authorize(Roles = Infrastructure.Constants.RoleConstants.Teacher)]
        [OpenApiOperation("Register Assistant", "Teacher creates an assistant account linked to their workspace (Tenant).")]
        public async Task<IActionResult> RegisterAssistantAsync([FromBody] RegisterAssistantRequest request)
        {
            var command = new RegisterAssistantCommand
            {
                Request = request,
                TeacherTenantId = Infrastructure.Identity.ClaimsPrincipalExtensions.GetTenant(User)!
            };

            var response = await Sender.Send(command);
            return Ok(response);
        }
    }
}