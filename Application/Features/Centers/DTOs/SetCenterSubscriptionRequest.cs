namespace Application.Features.Centers.DTOs
{
    /// <summary>
    /// Activates / renews a center subscription. <see cref="ExtendMonths"/> is added on top of
    /// the CURRENT expiry when the subscription is still active (so unused days aren't lost), or
    /// from today when it has already lapsed. This is the same money-math the future self-service
    /// payment flow will call — only the trigger changes.
    /// </summary>
    public class SetCenterSubscriptionRequest
    {
        public string TenantId { get; set; } = string.Empty;

        /// <summary>Months to add to the subscription. Must be >= 1.</summary>
        public int ExtendMonths { get; set; } = 1;

        /// <summary>Teacher seat limit. Null = unlimited.</summary>
        public int? MaxTeachers { get; set; }
    }
}
