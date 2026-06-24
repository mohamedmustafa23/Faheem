namespace Application.Features.Centers.DTOs
{
    /// <summary>Dashboard summary for a center owner.</summary>
    public class CenterOverviewDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // Subscription
        public bool IsActive { get; set; }
        public DateTime SubscriptionValidUntil { get; set; }
        public int SubscriptionDaysRemaining { get; set; }

        // Seats
        public int? MaxTeachers { get; set; }
        public int ActiveTeachers { get; set; }
        public int PendingInvites { get; set; }
    }
}
