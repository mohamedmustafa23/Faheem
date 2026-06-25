namespace Application.Features.Centers.DTOs
{
    /// <summary>Center owner creates a staff (employee) account scoped to their center.</summary>
    public class RegisterCenterStaffRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        /// <summary>Initial capability flags (CenterPermissions bitmask) the staff member gets.</summary>
        public int Permissions { get; set; }
    }
}
