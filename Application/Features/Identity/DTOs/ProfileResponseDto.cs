namespace Application.Features.Identity.DTOs
{
    public class ProfileResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string UserType { get; set; } = string.Empty;

        // Workspace/subscription info — populated for Teacher and Assistant accounts.
        public string? TenantName { get; set; }
        public DateTime? SubscriptionValidUntil { get; set; }
        public bool TenantIsActive { get; set; }

        /// <summary>Days remaining on the current subscription (null for non-teachers).</summary>
        public int? SubscriptionDaysRemaining
        {
            get
            {
                if (SubscriptionValidUntil is null) return null;
                var days = (SubscriptionValidUntil.Value.Date - DateTime.UtcNow.Date).Days;
                return days < 0 ? 0 : days;
            }
        }
    }
}
