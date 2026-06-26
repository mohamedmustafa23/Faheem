namespace Application.Features.Centers.DTOs
{
    public class PendingInviteDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string CenterName { get; set; } = string.Empty;
        public DateTime InvitedAt { get; set; }
    }
}
