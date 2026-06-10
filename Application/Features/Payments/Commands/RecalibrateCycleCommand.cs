using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Payments.Commands
{
    public class RecalibrateCycleCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid GroupId { get; set; }
    }

    public class RecalibrateCycleCommandHandler : IRequestHandler<RecalibrateCycleCommand, Result>
    {
        private readonly IPaymentService _paymentService;
        public RecalibrateCycleCommandHandler(IPaymentService paymentService) => _paymentService = paymentService;

        public async Task<Result> Handle(RecalibrateCycleCommand command, CancellationToken cancellationToken)
        {
            var message = await _paymentService.RecalibrateCurrentCycleAsync(
                command.GroupId, command.TenantId, cancellationToken);
            return Result.Success(message);
        }
    }
}
