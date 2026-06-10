using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Sessions.Commands
{
    public class DeleteSessionCommand : IRequest<Result>
    {
        [JsonIgnore]
        public string TenantId { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
    }

    public class DeleteSessionCommandHandler : IRequestHandler<DeleteSessionCommand, Result>
    {
        private readonly ISessionService _sessionService;
        public DeleteSessionCommandHandler(ISessionService sessionService) => _sessionService = sessionService;

        public async Task<Result> Handle(DeleteSessionCommand command, CancellationToken cancellationToken)
        {
            var result = await _sessionService.DeactivateScheduleAsync(command.SessionId, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
