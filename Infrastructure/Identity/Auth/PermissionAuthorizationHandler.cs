using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Constants;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Identity.Auth
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;

        public PermissionAuthorizationHandler(IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
        {
            _tenantAccessor = tenantAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var tenantInfo = _tenantAccessor.MultiTenantContext?.TenantInfo;
            if (tenantInfo?.Id == TenancyConstants.Root.Id)
            {
                if (HasPermission(context, requirement.Permission))
                    context.Succeed(requirement);
                return;
            }

            bool isGlobalUser = context.User.IsInRole(RoleConstants.Student) || context.User.IsInRole(RoleConstants.Parent);
            if (isGlobalUser)
            {
                if (HasPermission(context, requirement.Permission))
                    context.Succeed(requirement);
                return;
            }

            bool isTenantActive = tenantInfo?.IsActive == true;
            bool isTenantNotExpired = tenantInfo?.ValidUpTo >= DateTime.UtcNow;

            bool isWriteAction = requirement.Permission.Contains($".{AppAction.Create}") ||
                                 requirement.Permission.Contains($".{AppAction.Update}") ||
                                 requirement.Permission.Contains($".{AppAction.Delete}");

            if (isWriteAction && (!isTenantActive || !isTenantNotExpired))
            {
                return; 
            }

            if (HasPermission(context, requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }

        private static bool HasPermission(AuthorizationHandlerContext context, string permission)
        {
            return context.User.Claims.Any(c =>
                c.Type == ClaimConstants.Permission &&
                c.Value == permission);
        }
    }
}