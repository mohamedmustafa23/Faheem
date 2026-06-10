using Application.Features.Tenancy.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Tenancy.Queries
{
    public class GetTenantsQuery : IRequest<Result<List<TenantResponse>>> { }

    public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, Result<List<TenantResponse>>>
    {
        private readonly ITenantService _tenantService;

        public GetTenantsQueryHandler(ITenantService tenantService)
            => _tenantService = tenantService;

        public async Task<Result<List<TenantResponse>>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
        {
            var tenants = await _tenantService.GetTenantsAsync(cancellationToken);

            return Result<List<TenantResponse>>.Success(tenants);
        }
    }
}
