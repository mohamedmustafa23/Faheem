using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tenancy
{
    public class TenantDbSeeder : ITenantDbSeeder
    {
        private readonly TenantDbContext _tenantDbContext;
        private readonly IServiceProvider _serviceProvider;

        public TenantDbSeeder(
            TenantDbContext tenantDbContext,
            IServiceProvider serviceProvider)
        {
            _tenantDbContext = tenantDbContext;
            _serviceProvider = serviceProvider;
        }

        public async Task InitializeDatabaseAsync(CancellationToken ct = default)
        {
            if (_tenantDbContext.Database.GetMigrations().Any())
            {
                if ((await _tenantDbContext.Database.GetPendingMigrationsAsync(ct)).Any())
                {
                    await _tenantDbContext.Database.MigrateAsync(ct);
                }
            }

            await InitializeDatabaseWithTenantAsync(ct);

            using var scope = _serviceProvider.CreateScope();
            var appDbSeeder = scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>();
            await appDbSeeder.InitializeDatabaseAsync(ct);
        }

        private async Task InitializeDatabaseWithTenantAsync(CancellationToken ct = default)
        {
            var rootTenant = await _tenantDbContext.TenantInfo.FindAsync([TenancyConstants.Root.Id], ct);

            if (rootTenant is null)
            {
                rootTenant = new AppTenantInfo
                {
                    Id = TenancyConstants.Root.Id,
                    Identifier = TenancyConstants.Root.Id,
                    Name = TenancyConstants.Root.Name,
                    Email = TenancyConstants.Root.Email,
                    FirstName = TenancyConstants.Root.FirstName,
                    LastName = TenancyConstants.Root.LastName,
                    IsActive = true,
                    ValidUpTo = DateTime.UtcNow.AddYears(10)
                };

                await _tenantDbContext.TenantInfo.AddAsync(rootTenant, ct);
                await _tenantDbContext.SaveChangesAsync(ct);
            }
        }
    }
}