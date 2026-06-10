using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Tenancy.Commands
{
    public class DeactivateTenantCommand : IRequest<Result<string>>
    {
        public string TenantId { get; set; } = string.Empty;
    }

    public class DeactivateTenantCommandHandler : IRequestHandler<DeactivateTenantCommand, Result<string>>
    {
        private readonly ITenantService _tenantService;

        public DeactivateTenantCommandHandler(ITenantService tenantService)
            => _tenantService = tenantService;

        public async Task<Result<string>> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenantId = await _tenantService.DeactivateTenantAsync(request.TenantId, cancellationToken);

            return Result<string>.Success(tenantId, "Tenant deactivation successful");
        }
    }

}
