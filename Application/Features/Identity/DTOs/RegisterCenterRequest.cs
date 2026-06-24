namespace Application.Features.Identity.DTOs
{
    /// <summary>
    /// Self-registration for a standalone center account. The center owner is NOT a teacher —
    /// the account exists only to manage the center (members + subscription). A Center-type
    /// tenant is created inactive; it stays inactive until the subscription is activated
    /// (admin today, self-service payment later).
    /// </summary>
    public class RegisterCenterRequest
    {
        public string CenterName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
