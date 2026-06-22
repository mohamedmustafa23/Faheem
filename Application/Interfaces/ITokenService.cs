using Application.Features.Tokens.DTOs;

namespace Application.Interfaces
{
    public interface ITokenService
    {
        Task<TokenResponse> LoginAsync(TokenRequest request);
        Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);

        /// <summary>
        /// Exchanges an authenticated session (typically the account token returned by
        /// login when the user has multiple workspaces) for a full access token scoped
        /// to the chosen workspace. Also used to switch workspace later without re-login.
        /// </summary>
        Task<TokenResponse> SelectWorkspaceAsync(string userId, string tenantId);

        Task LogoutAsync(string userId, string refreshToken);
    }
}
