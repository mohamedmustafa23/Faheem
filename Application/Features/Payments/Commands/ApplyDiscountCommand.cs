using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Commands
{
    public class ApplyDiscountCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid RecordId { get; set; }

        public decimal Amount { get; set; }
        public string? Reason { get; set; }
    }

    public class ApplyDiscountCommandHandler : IRequestHandler<ApplyDiscountCommand, Result>
    {
        private readonly IPaymentService _paymentService;
        public ApplyDiscountCommandHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result> Handle(ApplyDiscountCommand command, CancellationToken cancellationToken)
        {
            var message = await _paymentService.ApplyDiscountAsync(
                command.RecordId, command.Amount, command.Reason, command.TenantId, cancellationToken);
            return Result.Success(message);
        }
    }
}
