namespace Application.Features.Tokens.DTOs
{
    public class TokenRequest
    {
        public string PhoneNumberOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
