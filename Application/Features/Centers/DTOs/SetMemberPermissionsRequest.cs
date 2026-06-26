namespace Application.Features.Centers.DTOs
{
    public class SetMemberPermissionsRequest
    {
        /// <summary>The member's new capability flags (CenterPermissions bitmask).</summary>
        public int Permissions { get; set; }
    }
}
