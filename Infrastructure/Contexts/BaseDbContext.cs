using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Identity.Models;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Contexts
{
    public class BaseDbContext : IdentityDbContext<
            ApplicationUser,
            ApplicationRole,
            string,
            IdentityUserClaim<string>,
            IdentityUserRole<string>,
            IdentityUserLogin<string>,
            ApplicationRoleClaim,
            IdentityUserToken<string>>
    {
        private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;

        public BaseDbContext(
            IMultiTenantContextAccessor<AppTenantInfo> tenantContextAccessor,
            DbContextOptions options)
            : base(options) 
        {
            _tenantAccessor = tenantContextAccessor;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            var tenant = _tenantAccessor.MultiTenantContext?.TenantInfo;
            if (!string.IsNullOrEmpty(tenant?.ConnectionString))
            {
                optionsBuilder.UseSqlServer(tenant.ConnectionString, options =>
                {
                    options.MigrationsAssembly(
                        Assembly.GetExecutingAssembly().FullName);
                });

            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        }
    }
}