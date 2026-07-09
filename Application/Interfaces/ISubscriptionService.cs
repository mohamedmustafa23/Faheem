using Application.Features.Tenancy.DTOs;

namespace Application.Interfaces
{
    // The single source of truth for subscription state + lifecycle. Every caller
    // funnels through here — the admin panel today, and (later) the website
    // self-serve flow + a Paymob webhook — so activation logic lives in ONE place.
    public interface ISubscriptionService
    {
        /// <summary>The given workspace's current subscription status (for the renewal banner).</summary>
        Task<SubscriptionStatusDto?> GetStatusAsync(string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Extends a subscription by N months (from today if already expired) and
        /// reactivates it. For centers, <paramref name="maxTeachers"/> sets the seat
        /// limit in the same step (price = seats × duration). This is the activation
        /// seam — manual renewal, a payment webhook, and self-serve renewal all call it.
        /// </summary>
        Task ExtendAsync(string tenantId, int months, int? maxTeachers = null, CancellationToken ct = default);

        Task SetActiveAsync(string tenantId, bool isActive, CancellationToken ct = default);

        /// <summary>
        /// Notifies the owners of any subscriptions hitting a reminder threshold
        /// (7 / 3 / 1 / 0 days left). Driven daily by a background job. Returns how
        /// many owners were notified.
        /// </summary>
        Task<int> SendDueRemindersAsync(CancellationToken ct = default);
    }
}
