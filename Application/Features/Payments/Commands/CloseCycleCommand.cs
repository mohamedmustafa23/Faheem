using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Commands
{
    public class CloseCycleCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid CycleId { get; set; }
    }

    public class CloseCycleCommandHandler : IRequestHandler<CloseCycleCommand, Result>
    {
        private readonly IPaymentService _paymentService;
        public CloseCycleCommandHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result> Handle(CloseCycleCommand command, CancellationToken cancellationToken)
        {
            var message = await _paymentService.CloseCycleManuallyAsync(command.CycleId, command.TenantId, cancellationToken);
            return Result.Success(message);
        }
    }
}
