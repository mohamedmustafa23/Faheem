using Application.Exceptions;
using Application.Features.Tenancy.DTOs;
using Application.Interfaces;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Contexts;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tenancy
{
    public class TenantService : ITenantService
    {
        private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
        private readonly IServiceProvider _serviceProvider;

        public TenantService(
            IMultiTenantStore<AppTenantInfo> tenantStore,
            IServiceProvider serviceProvider)
        {
            _tenantStore = tenantStore;
            _serviceProvider = serviceProvider;
        }

        // ══════════════════════════════════════════════════
        // Create Tenant & Seed its Database
        // ══════════════════════════════════════════════════
        public async Task<string> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default)
        {
            var newTenant = new AppTenantInfo
            {
                Id = request.Identifier,
                Identifier = request.Identifier,
                Name = request.Name,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ConnectionString = request.ConnectionString,
                ValidUpTo = request.ValidUpTo,
                IsActive = true
            };

            await _tenantStore.TryAddAsync(newTenant);


            using (var scope = _serviceProvider.CreateScope())
            {

                var contextSetter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
                contextSetter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>
                {
                    TenantInfo = newTenant
                };

                var appDbSeeder = scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>();
                await appDbSeeder.InitializeDatabaseAsync(ct);
            }

            return newTenant.Id;
        }

        // ══════════════════════════════════════════════════
        // Activate Tenant
        // ══════════════════════════════════════════════════
        public async Task<string> ActivateTenantAsync(string tenantId, CancellationToken ct = default)
        {
            var tenant = await _tenantStore.TryGetAsync(tenantId)
                ?? throw new NotFoundException(["Tenant not found."]);

            tenant.IsActive = true;
            await _tenantStore.TryUpdateAsync(tenant);

            return tenant.Id;
        }

        // ══════════════════════════════════════════════════
        // Deactivate Tenant
        // ══════════════════════════════════════════════════
        public async Task<string> DeactivateTenantAsync(string tenantId, CancellationToken ct = default)
        {
            var tenant = await _tenantStore.TryGetAsync(tenantId)
                ?? throw new NotFoundException(["Tenant not found."]);

            tenant.IsActive = false;
            await _tenantStore.TryUpdateAsync(tenant);

            return tenant.Id;
        }

        // ══════════════════════════════════════════════════
        // Update Subscription (ValidUpTo)
        // ══════════════════════════════════════════════════
        public async Task<string> UpdateSubscriptionAsync(UpdateTenantSubscriptionRequest request, CancellationToken ct = default)
        {
            var tenant = await _tenantStore.TryGetAsync(request.TenantId)
                ?? throw new NotFoundException(["Tenant not found."]);

            tenant.ValidUpTo = request.ValidUpTo;
            await _tenantStore.TryUpdateAsync(tenant);

            return tenant.Id;
        }

        // ══════════════════════════════════════════════════
        // Get Tenant By Id
        // ══════════════════════════════════════════════════
        public async Task<TenantResponse?> GetTenantByIdAsync(string tenantId, CancellationToken ct = default)
        {
            var tenant = await _tenantStore.TryGetAsync(tenantId);
            if (tenant is null) return null;

            return tenant.Adapt<TenantResponse>();
        }

        // ══════════════════════════════════════════════════
        // Get All Tenants
        // ══════════════════════════════════════════════════
        public async Task<List<TenantResponse>> GetTenantsAsync(CancellationToken ct = default)
        {
            var tenants = await _tenantStore.GetAllAsync();
            return tenants.Adapt<List<TenantResponse>>();
        }

        // ══════════════════════════════════════════════════
        // Delete Tenant (Hard Delete for Unverified Users)
        // ══════════════════════════════════════════════════
        public async Task<bool> DeleteTenantAsync(string tenantId)
        {
            return await _tenantStore.TryRemoveAsync(tenantId);
        }
    }
}