using Infrastructure.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Infrastructure.Identity
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetEmail(this ClaimsPrincipal principal) =>
            principal.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? principal.FindFirstValue(ClaimTypes.Email);

        public static string? GetUserId(this ClaimsPrincipal principal) =>
            principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        public static string? GetTenant(this ClaimsPrincipal principal) =>
            principal.FindFirstValue(ClaimConstants.Tenant);

        public static string? GetFirstName(this ClaimsPrincipal principal) =>
            principal.FindFirstValue(ClaimTypes.GivenName);

        public static string? GetLastName(this ClaimsPrincipal principal) =>
            principal.FindFirstValue(ClaimTypes.Surname);

        public static string? GetPhoneNumber(this ClaimsPrincipal principal) =>
            principal.FindFirstValue(ClaimTypes.MobilePhone);
    }
}