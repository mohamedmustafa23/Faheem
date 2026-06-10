using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Tenancy.Commands
{
    public class ActivateTenantCommand : IRequest<Result<string>>
    {
        public string TenantId { get; set; } = string.Empty;
    }

    public class ActivateTenantCommandHandler : IRequestHandler<ActivateTenantCommand, Result<string>>
    {
        private readonly ITenantService _tenantService;

        public ActivateTenantCommandHandler(ITenantService tenantService)
            => _tenantService = tenantService;

        public async Task<Result<string>> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenantId = await _tenantService.ActivateTenantAsync(request.TenantId, cancellationToken);

            return Result<string>.Success(tenantId, "Tenant activation successful");
        }
    }
}
