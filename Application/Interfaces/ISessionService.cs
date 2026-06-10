using Application.Features.Sessions.DTOs;

namespace Application.Interfaces
{
    public interface ISessionService
    {
        Task<string> CreateSchedulesAsync(CreateSessionRequest request, string tenantId, CancellationToken ct = default);
        Task<string> UpdateScheduleAsync(UpdateSessionRequest request, string tenantId, CancellationToken ct = default);
        Task<string> DeactivateScheduleAsync(Guid scheduleId, string tenantId, CancellationToken ct = default);
        Task<List<TodaySessionResponseDto>> GetTodaySessionsAsync(string tenantId, DateOnly today, bool includePending, CancellationToken ct = default);
        Task<string> CancelOccurrenceAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default);
        Task<string> CreateManualOccurrenceAsync(CreateManualOccurrenceRequest request, string tenantId, CancellationToken ct = default);
        Task<string> DeleteManualOccurrenceAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default);
        Task<string> UpdateManualOccurrenceAsync(Guid occurrenceId, UpdateManualOccurrenceRequest request, string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Edit a single occurrence that came from a recurring schedule —
        /// reschedules just THIS one (date/time). The schedule itself is
        /// unchanged: next week still fires on the original day/time.
        /// </summary>
        Task<string> UpdateRecurringOccurrenceAsync(Guid occurrenceId, UpdateManualOccurrenceRequest request, string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Physically remove a single occurrence from a recurring schedule.
        /// No payment side-effects (recurring sessions price through the group
        /// MonthlyFee, not per-occurrence). If no other future scheduled
        /// occurrence exists on the same schedule, the next week's occurrence
        /// is auto-generated so the chain stays alive.
        /// </summary>
        Task<string> DeleteRecurringOccurrenceAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default);
    }
}
