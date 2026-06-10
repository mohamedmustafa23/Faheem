using Application.Features.Tenancy.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ITenantService
    {
        Task<string> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default);

        Task<string> ActivateTenantAsync(string tenantId, CancellationToken ct = default);

        Task<string> DeactivateTenantAsync(string tenantId, CancellationToken ct = default);

        Task<string> UpdateSubscriptionAsync(UpdateTenantSubscriptionRequest request, CancellationToken ct = default);

        Task<TenantResponse?> GetTenantByIdAsync(string tenantId, CancellationToken ct = default);

        Task<List<TenantResponse>> GetTenantsAsync(CancellationToken ct = default);
        Task<bool> DeleteTenantAsync(string tenantId);
    }
}
