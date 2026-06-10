using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Sessions.Commands
{
    public class CancelSessionCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid OccurrenceId { get; set; }
    }

    public class CancelSessionCommandHandler : IRequestHandler<CancelSessionCommand, Result>
    {
        private readonly ISessionService _sessionService;
        public CancelSessionCommandHandler(ISessionService sessionService) => _sessionService = sessionService;

        public async Task<Result> Handle(CancelSessionCommand command, CancellationToken cancellationToken)
        {
            var result = await _sessionService.CancelOccurrenceAsync(command.OccurrenceId, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
