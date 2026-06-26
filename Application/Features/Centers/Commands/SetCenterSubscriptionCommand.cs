using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Centers.Commands
{
    public class SetCenterSubscriptionCommand : IRequest<Result<DateTime>>
    {
        public SetCenterSubscriptionRequest Request { get; set; } = new();
    }

    public class SetCenterSubscriptionCommandHandler : IRequestHandler<SetCenterSubscriptionCommand, Result<DateTime>>
    {
        private readonly ICenterService _centerService;
        public SetCenterSubscriptionCommandHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<DateTime>> Handle(SetCenterSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var validUntil = await _centerService.SetCenterSubscriptionAsync(request.Request, cancellationToken);
            return Result<DateTime>.Success(validUntil, "Center subscription updated.");
        }
    }
}
