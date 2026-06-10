using Application.Features.Tenancy.DTOs; 
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Tenancy.Queries
{
    public class GetTenantByIdQuery : IRequest<Result<TenantResponse>>
    {
        public string TenantId { get; set; } = string.Empty;
    }

    public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, Result<TenantResponse>>
    {
        private readonly ITenantService _tenantService;

        public GetTenantByIdQueryHandler(ITenantService tenantService) => _tenantService = tenantService;

        public async Task<Result<TenantResponse>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
        {
            var tenant = await _tenantService.GetTenantByIdAsync(request.TenantId, cancellationToken);

            if (tenant is not null)
            {
                return Result<TenantResponse>.Success(tenant, "Tenant retrieved successfully.");
            }
            return Result<TenantResponse>.Failure("Tenant does not exist.");
        }
    }
}