using Application.Interfaces;
using Infrastructure.Constants;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Identity.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? TenantId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimConstants.Tenant);

        public bool IsGlobalUser
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null) return false;

                return user.IsInRole(RoleConstants.Student) || user.IsInRole(RoleConstants.Parent);
            }
        }
    }
}