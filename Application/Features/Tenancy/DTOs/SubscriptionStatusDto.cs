namespace Application.Features.Tenancy.DTOs
{
    // The logged-in owner's own subscription state — powers the in-app renewal
    // banner and (later) the self-serve renew flow.
    public class SubscriptionStatusDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Individual";
        public bool IsActive { get; set; }
        public DateTime ValidUntil { get; set; }
        public int DaysLeft { get; set; }

        /// <summary>"active" | "expiring" | "expired" | "suspended".</summary>
        public string Status { get; set; } = "active";
    }
}
