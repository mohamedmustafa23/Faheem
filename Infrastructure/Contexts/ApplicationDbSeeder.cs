using Domain.Enums;
using Infrastructure.Constants;
using Infrastructure.Identity.Models;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Infrastructure.Contexts
{
    public class ApplicationDbSeeder
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IConfiguration _configuration;

        public ApplicationDbSeeder(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext applicationDbContext,
            IConfiguration configuration)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _applicationDbContext = applicationDbContext;
            _configuration = configuration;
        }

        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
        {
            if (_applicationDbContext.Database.GetMigrations().Any())
            {
                if ((await _applicationDbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                {
                    await _applicationDbContext.Database.MigrateAsync(cancellationToken);
                }
            }

            if (await _applicationDbContext.Database.CanConnectAsync(cancellationToken))
            {
                await InitializeDefaultRolesAsync(cancellationToken);
                await SeedAdminUserAsync(cancellationToken);
            }
        }

        private async Task InitializeDefaultRolesAsync(CancellationToken ct = default)
        {
            foreach (var roleName in RoleConstants.DefaultRoles)
            {
                var incomingRole = await _roleManager.Roles.SingleOrDefaultAsync(r => r.Name == roleName, ct)
                    ?? new ApplicationRole { Name = roleName, Description = $"{roleName} Role" };

                if (!await _roleManager.RoleExistsAsync(roleName))
                    await _roleManager.CreateAsync(incomingRole);

                var savedRole = await _roleManager.Roles.SingleOrDefaultAsync(r => r.Name == roleName, ct);
                if (savedRole is null) continue;

                if (roleName == RoleConstants.Admin)
                {
                    await AssignPermissionsToRoleAsync(AppPermissions.Admin, savedRole, ct);
                    await AssignPermissionsToRoleAsync(AppPermissions.Root, savedRole, ct);
                }
                else if (roleName == RoleConstants.Teacher)
                    await AssignPermissionsToRoleAsync(AppPermissions.Teacher, savedRole, ct);
                else if (roleName == RoleConstants.Student)
                    await AssignPermissionsToRoleAsync(AppPermissions.Student, savedRole, ct);
                else if (roleName == RoleConstants.Parent)
                    await AssignPermissionsToRoleAsync(AppPermissions.Parent, savedRole, ct);
            }
        }

        private async Task AssignPermissionsToRoleAsync(IReadOnlyList<AppPermission> rolePermissions, ApplicationRole currentRole, CancellationToken ct = default)
        {
            var currentlyAssignedClaims = await _roleManager.GetClaimsAsync(currentRole);
            var assignedValues = currentlyAssignedClaims
                .Where(c => c.Type == ClaimConstants.Permission)
                .Select(c => c.Value)
                .ToHashSet();

            var newClaims = rolePermissions
                .Where(p => !assignedValues.Contains(p.Name))
                .Select(p => new ApplicationRoleClaim
                {
                    RoleId = currentRole.Id,
                    ClaimType = ClaimConstants.Permission,
                    ClaimValue = p.Name,
                    Description = p.Description,
                    Group = p.Group
                })
                .ToList();

            if (newClaims.Any())
            {
                await _applicationDbContext.RoleClaims.AddRangeAsync(newClaims, ct);
                await _applicationDbContext.SaveChangesAsync(ct);
            }
        }

        private async Task SeedAdminUserAsync(CancellationToken ct = default)
        {
            string adminEmail = TenancyConstants.Root.Email;
            string adminPassword = _configuration["AdminSettings:DefaultPassword"]
                ?? throw new InvalidOperationException("AdminSettings:DefaultPassword is not configured. Set it via environment variable or appsettings.");

            var incomingUser = await _userManager.Users.SingleOrDefaultAsync(u => u.Email == adminEmail, ct)
                ?? new ApplicationUser
                {
                    FirstName = TenancyConstants.Root.FirstName,
                    LastName = TenancyConstants.Root.LastName,
                    Email = adminEmail,
                    UserName = adminEmail,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    NormalizedEmail = adminEmail.ToUpperInvariant(),
                    NormalizedUserName = adminEmail.ToUpperInvariant(),
                    IsActive = true,
                    UserType = UserType.SystemAdmin
                };

            if (!await _userManager.Users.AnyAsync(u => u.Email == adminEmail, ct))
            {
                var passwordHasher = new PasswordHasher<ApplicationUser>();
                incomingUser.PasswordHash = passwordHasher.HashPassword(incomingUser, adminPassword);
                await _userManager.CreateAsync(incomingUser);

                await _userManager.AddClaimAsync(incomingUser, new Claim(ClaimConstants.Tenant, TenancyConstants.Root.Id));
            }

            if (!await _userManager.IsInRoleAsync(incomingUser, RoleConstants.Admin))
            {
                await _userManager.AddToRoleAsync(incomingUser, RoleConstants.Admin);
            }
        }
    }
}