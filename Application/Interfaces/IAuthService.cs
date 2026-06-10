using Application.Features.Identity.Commands;
using Application.Features.Identity.DTOs;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterTeacherAsync(RegisterTeacherRequest request, CancellationToken ct = default);
        Task<string> RegisterStudentAsync(RegisterStudentRequest request, CancellationToken ct = default);
        Task<string> RegisterParentAsync(RegisterParentRequest request, CancellationToken ct = default);

        // OTP
        Task GenerateAndSendOtpAsync(string email, CancellationToken ct = default);
        Task<string> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);

        // Password
        Task ForgotPasswordAsync(string email, CancellationToken ct = default);
        Task<string> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
        Task<string> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default);

        // Assistant
        Task<string> RegisterAssistantAsync(RegisterAssistantRequest request, string teacherTenantId, CancellationToken ct = default);
        Task<List<AssistantDto>> GetTeacherAssistantsAsync(string teacherTenantId, CancellationToken ct = default);
        Task<string> RemoveAssistantAsync(string assistantUserId, string teacherTenantId, CancellationToken ct = default);

        // Profile
        Task<ProfileResponseDto> GetProfileAsync(string userId, CancellationToken ct = default);
        Task<string> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default);

        /// <summary>
        /// Hard-deletes the current user's account: removes the Identity user
        /// and any owned tenant data. Refuses for teacher accounts that still
        /// own active groups so a careless tap doesn't strand other users.
        /// </summary>
        Task<string> DeleteMyAccountAsync(string userId, CancellationToken ct = default);
    }
}

