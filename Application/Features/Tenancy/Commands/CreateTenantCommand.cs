using Application.Features.Tenancy.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Tenancy.Commands
{
    public class CreateTenantCommand : IRequest<Result<string>>
    {
        public CreateTenantRequest CreateTenantRequest { get; set; } = new();
    }

    public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<string>>
    {
        private readonly ITenantService _tenantService;

        public CreateTenantCommandHandler(ITenantService tenantService)
            => _tenantService = tenantService;

        public async Task<Result<string>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenantId = await _tenantService.CreateTenantAsync(request.CreateTenantRequest, cancellationToken);

            return Result<string>.Success(tenantId, "Tenant created successfully");
        }
    }

}
