using Application.Features.Tenancy.DTOs;

namespace Application.Interfaces
{
    // The admin control center: read every subscriber with live data, and manage
    // their subscription (extend / activate). Works across all tenants.
    public interface IAdminService
    {
        Task<List<AdminSubscriberDto>> GetSubscribersAsync(CancellationToken ct = default);
        Task<AdminSubscriberDto?> GetSubscriberByIdAsync(string id, CancellationToken ct = default);

        /// <summary>Extends the subscription by N months and reactivates it. For centers, optionally sets the seat limit in the same step.</summary>
        Task<AdminSubscriberDto> ExtendSubscriptionAsync(string id, int months, int? maxTeachers = null, CancellationToken ct = default);

        /// <summary>Activates or suspends a subscriber.</summary>
        Task<AdminSubscriberDto> SetActiveAsync(string id, bool isActive, CancellationToken ct = default);

        /// <summary>Sets a center's teacher seat limit (null = unlimited). Centers only.</summary>
        Task<AdminSubscriberDto> SetCenterSeatsAsync(string id, int? maxTeachers, CancellationToken ct = default);

        /// <summary>Removes a subscriber's workspace. Blocked if it still has groups (to avoid orphaning data).</summary>
        Task DeleteSubscriberAsync(string id, CancellationToken ct = default);
    }
}
