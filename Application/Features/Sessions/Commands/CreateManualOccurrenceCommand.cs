using Application.Features.Sessions.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Sessions.Commands
{
    public class CreateManualOccurrenceCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public CreateManualOccurrenceRequest Request { get; set; } = new();
    }

    public class CreateManualOccurrenceCommandHandler : IRequestHandler<CreateManualOccurrenceCommand, Result>
    {
        private readonly ISessionService _sessionService;
        public CreateManualOccurrenceCommandHandler(ISessionService sessionService) => _sessionService = sessionService;

        public async Task<Result> Handle(CreateManualOccurrenceCommand command, CancellationToken cancellationToken)
        {
            var result = await _sessionService.CreateManualOccurrenceAsync(command.Request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
