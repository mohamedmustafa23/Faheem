using Application.Features.Sessions.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Sessions.Queries
{
    public class GetTodayScheduleQuery : IRequest<Result<List<TodaySessionResponseDto>>>
    {
        [JsonIgnore]
        public string TenantId { get; set; } = string.Empty;
        public DateOnly? TodayDate { get; set; }
        /// <summary>When true, also returns past-dated occurrences still in Scheduled status (overdue / forgotten).</summary>
        public bool IncludePending { get; set; } = false;
    }

    public class GetTodayScheduleQueryHandler : IRequestHandler<GetTodayScheduleQuery, Result<List<TodaySessionResponseDto>>>
    {
        private readonly ISessionService _sessionService;
        private readonly IDateTimeService _dateTime;
        public GetTodayScheduleQueryHandler(ISessionService sessionService, IDateTimeService dateTime)
        {
            _sessionService = sessionService;
            _dateTime = dateTime;
        }

        public async Task<Result<List<TodaySessionResponseDto>>> Handle(GetTodayScheduleQuery query, CancellationToken cancellationToken)
        {
            var today = query.TodayDate ?? _dateTime.TodayInAppZone;
            var sessions = await _sessionService.GetTodaySessionsAsync(query.TenantId, today, query.IncludePending, cancellationToken);
            return Result<List<TodaySessionResponseDto>>.Success(sessions);
        }
    }
}
