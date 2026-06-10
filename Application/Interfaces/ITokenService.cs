using Application.Features.Tokens.DTOs;

namespace Application.Interfaces
{
    public interface ITokenService
    {
        Task<TokenResponse> LoginAsync(TokenRequest request);
        Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(string userId, string refreshToken);
    }
}
