namespace Application.Features.Tokens.DTOs
{
    public class RefreshTokenRequest
    {
        public string CurrentJwtToken { get; set; } = string.Empty;
        public string CurrentRefreshToken { get; set; } = string.Empty;
    }
}
