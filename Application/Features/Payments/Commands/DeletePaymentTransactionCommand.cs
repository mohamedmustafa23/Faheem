using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Commands
{
    public class DeletePaymentTransactionCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid TransactionId { get; set; }
    }

    public class DeletePaymentTransactionCommandHandler : IRequestHandler<DeletePaymentTransactionCommand, Result>
    {
        private readonly IPaymentService _paymentService;
        public DeletePaymentTransactionCommandHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result> Handle(DeletePaymentTransactionCommand command, CancellationToken cancellationToken)
        {
            var message = await _paymentService.DeletePaymentTransactionAsync(command.TransactionId, command.TenantId, cancellationToken);
            return Result.Success(message);
        }
    }
}
