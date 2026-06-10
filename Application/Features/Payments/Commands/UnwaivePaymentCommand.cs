using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Commands
{
    public class UnwaivePaymentCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid RecordId { get; set; }
    }

    public class UnwaivePaymentCommandHandler : IRequestHandler<UnwaivePaymentCommand, Result>
    {
        private readonly IPaymentService _paymentService;
        public UnwaivePaymentCommandHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result> Handle(UnwaivePaymentCommand command, CancellationToken cancellationToken)
        {
            var message = await _paymentService.UnwaivePaymentRecordAsync(command.RecordId, command.TenantId, cancellationToken);
            return Result.Success(message);
        }
    }
}
