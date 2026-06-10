using Application.Features.Sessions.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Sessions.Commands
{
    public class CreateSessionCommand : IRequest<Result>
    {
        [JsonIgnore]
        public string TenantId { get; set; } = string.Empty;
        public CreateSessionRequest Request { get; set; } = new();
    }

    public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, Result>
    {
        private readonly ISessionService _sessionService;

        public CreateSessionCommandHandler(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task<Result> Handle(CreateSessionCommand command, CancellationToken cancellationToken)
        {
            var result = await _sessionService.CreateSchedulesAsync(command.Request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}