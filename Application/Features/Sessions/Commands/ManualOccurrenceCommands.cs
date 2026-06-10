using Application.Features.Sessions.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Sessions.Commands
{
    // ────────────────────────────────────────────────────────────────────────
    //  Delete manual occurrence
    // ────────────────────────────────────────────────────────────────────────
    public class DeleteManualOccurrenceCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid OccurrenceId { get; set; }
    }

    public class DeleteManualOccurrenceCommandHandler : IRequestHandler<DeleteManualOccurrenceCommand, Result>
    {
        private readonly ISessionService _sessionService;
        public DeleteManualOccurrenceCommandHandler(ISessionService sessionService) => _sessionService = sessionService;

        public async Task<Result> Handle(DeleteManualOccurrenceCommand command, CancellationToken cancellationToken)
        {
            var result = await _sessionService.DeleteManualOccurrenceAsync(command.OccurrenceId, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Update manual occurrence (date / time only)
    // ────────────────────────────────────────────────────────────────────────
    public class UpdateManualOccurrenceCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid OccurrenceId { get; set; }
        public DateOnly OccurrenceDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class UpdateManualOccurrenceCommandHandler : IRequestHandler<UpdateManualOccurrenceCommand, Result>
    {
        private readonly ISessionService _sessionService;
        public UpdateManualOccurrenceCommandHandler(ISessionService sessionService) => _sessionService = sessionService;

        public async Task<Result> Handle(UpdateManualOccurrenceCommand command, CancellationToken cancellationToken)
        {
            var request = new UpdateManualOccurrenceRequest
            {
                OccurrenceDate = command.OccurrenceDate,
                StartTime      = command.StartTime,
                EndTime        = command.EndTime,
            };
            var result = await _sessionService.UpdateManualOccurrenceAsync(command.OccurrenceId, request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Delete a single recurring occurrence
    // ────────────────────────────────────────────────────────────────────────
    public class DeleteRecurringOccurrenceCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid OccurrenceId { get; set; }
    }

    public class DeleteRecurringOccurrenceCommandHandler : IRequestHandler<DeleteRecurringOccurrenceCommand, Result>
    {
        private readonly ISessionService _sessionService;
        public DeleteRecurringOccurrenceCommandHandler(ISessionService sessionService) => _sessionService = sessionService;

        public async Task<Result> Handle(DeleteRecurringOccurrenceCommand command, CancellationToken cancellationToken)
        {
            var result = await _sessionService.DeleteRecurringOccurrenceAsync(command.OccurrenceId, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Update a single recurring occurrence (this week only — schedule untouched)
    // ────────────────────────────────────────────────────────────────────────
    public class UpdateRecurringOccurrenceCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public Guid OccurrenceId { get; set; }
        public DateOnly OccurrenceDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class UpdateRecurringOccurrenceCommandHandler : IRequestHandler<UpdateRecurringOccurrenceCommand, Result>
    {
        private readonly ISessionService _sessionService;
        public UpdateRecurringOccurrenceCommandHandler(ISessionService sessionService) => _sessionService = sessionService;

        public async Task<Result> Handle(UpdateRecurringOccurrenceCommand command, CancellationToken cancellationToken)
        {
            var request = new UpdateManualOccurrenceRequest
            {
                OccurrenceDate = command.OccurrenceDate,
                StartTime      = command.StartTime,
                EndTime        = command.EndTime,
            };
            var result = await _sessionService.UpdateRecurringOccurrenceAsync(command.OccurrenceId, request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
