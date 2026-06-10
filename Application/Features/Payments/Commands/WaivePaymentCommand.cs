using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Commands
{
    public class WaivePaymentCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid RecordId { get; set; }
    }

    public class WaivePaymentCommandHandler : IRequestHandler<WaivePaymentCommand, Result>
    {
        private readonly IPaymentService _paymentService;
        public WaivePaymentCommandHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result> Handle(WaivePaymentCommand command, CancellationToken cancellationToken)
        {
            var message = await _paymentService.WaivePaymentRecordAsync(command.RecordId, command.TenantId, cancellationToken);
            return Result.Success(message);
        }
    }
}
