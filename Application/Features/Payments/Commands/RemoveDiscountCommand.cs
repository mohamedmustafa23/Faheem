using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Commands
{
    public class RemoveDiscountCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid RecordId { get; set; }
    }

    public class RemoveDiscountCommandHandler : IRequestHandler<RemoveDiscountCommand, Result>
    {
        private readonly IPaymentService _paymentService;
        public RemoveDiscountCommandHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result> Handle(RemoveDiscountCommand command, CancellationToken cancellationToken)
        {
            var message = await _paymentService.RemoveDiscountAsync(
                command.RecordId, command.TenantId, cancellationToken);
            return Result.Success(message);
        }
    }
}
