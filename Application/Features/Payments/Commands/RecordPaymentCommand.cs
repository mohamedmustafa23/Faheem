using Application.Features.Payments.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Commands
{
    public class RecordPaymentCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid RecordId { get; set; }

        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Notes { get; set; }
    }

    public class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Result>
    {
        private readonly IPaymentService _paymentService;
        public RecordPaymentCommandHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result> Handle(RecordPaymentCommand command, CancellationToken cancellationToken)
        {
            var request = new RecordPaymentRequest
            {
                RecordId = command.RecordId,
                Amount   = command.Amount,
                PaidAt   = command.PaidAt,
                Notes    = command.Notes
            };
            var message = await _paymentService.RecordPaymentAsync(request, command.TenantId, cancellationToken);
            return Result.Success(message);
        }
    }
}
