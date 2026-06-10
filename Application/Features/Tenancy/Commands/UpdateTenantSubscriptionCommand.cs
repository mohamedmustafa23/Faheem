using Application.Features.Tenancy.DTOs;
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
    public class UpdateTenantSubscriptionCommand : IRequest<Result>
    {
        public UpdateTenantSubscriptionRequest Request { get; set; } = new();
    }

    public class UpdateTenantSubscriptionCommandHandler : IRequestHandler<UpdateTenantSubscriptionCommand, Result>
    {
        private readonly ITenantService _tenantService;

        public UpdateTenantSubscriptionCommandHandler(ITenantService tenantService)
            => _tenantService = tenantService;

        public async Task<Result> Handle(UpdateTenantSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var tenantId = await _tenantService.UpdateSubscriptionAsync(request.Request, cancellationToken);

            return Result<string>.Success(tenantId, "Tenant subscription updated successfully");
        }
    }
}
