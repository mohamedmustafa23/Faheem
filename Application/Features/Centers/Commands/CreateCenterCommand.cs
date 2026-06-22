using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Centers.Commands
{
    public class CreateCenterCommand : IRequest<Result<string>>
    {
        public CreateCenterRequest Request { get; set; } = new();
    }

    public class CreateCenterCommandHandler : IRequestHandler<CreateCenterCommand, Result<string>>
    {
        private readonly ICenterService _centerService;
        public CreateCenterCommandHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<string>> Handle(CreateCenterCommand request, CancellationToken cancellationToken)
        {
            var tenantId = await _centerService.CreateCenterAsync(request.Request, cancellationToken);
            return Result<string>.Success(tenantId, "Center created successfully.");
        }
    }
}
