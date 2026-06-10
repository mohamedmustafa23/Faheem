using Application.Features.Sessions.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Sessions.Commands
{
    public class UpdateSessionCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;

        public Guid ScheduleId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class UpdateSessionCommandHandler : IRequestHandler<UpdateSessionCommand, Result>
    {
        private readonly ISessionService _sessionService;
        public UpdateSessionCommandHandler(ISessionService sessionService) => _sessionService = sessionService;

        public async Task<Result> Handle(UpdateSessionCommand command, CancellationToken cancellationToken)
        {
            var request = new UpdateSessionRequest
            {
                ScheduleId = command.ScheduleId,
                DayOfWeek = command.DayOfWeek,
                StartTime = command.StartTime,
                EndTime = command.EndTime
            };

            var result = await _sessionService.UpdateScheduleAsync(request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
