using Application.Features.Centers.DTOs;

namespace Application.Interfaces
{
    public interface ICenterService
    {
        /// <summary>Center owner: invites an existing user to join the center as a teacher (Status = Invited).</summary>
        Task<string> InviteTeacherAsync(string tenantId, string ownerUserId, InviteTeacherRequest request, CancellationToken ct = default);

        /// <summary>Invited user: accepts (Status → Active) or declines (membership removed) a center invite.</summary>
        Task<string> RespondToInviteAsync(string userId, string tenantId, bool accept, CancellationToken ct = default);

        /// <summary>The pending center invites awaiting the current user's response.</summary>
        Task<List<PendingInviteDto>> GetMyInvitesAsync(string userId, CancellationToken ct = default);

        /// <summary>Center owner: lists every member (and pending invite) of the center.</summary>
        Task<List<CenterMemberDto>> GetCenterMembersAsync(string tenantId, string ownerUserId, CancellationToken ct = default);

        /// <summary>Center owner: removes a member (or pending invite) from the center.</summary>
        Task<string> RemoveMemberAsync(string tenantId, string ownerUserId, string memberUserId, CancellationToken ct = default);

        /// <summary>Center owner: dashboard summary — name, subscription status, seats, member counts.</summary>
        Task<CenterOverviewDto> GetCenterOverviewAsync(string tenantId, string ownerUserId, CancellationToken ct = default);

        /// <summary>Admin/payment: activates or renews a center subscription (seat limit + extension). Returns the new valid-until date.</summary>
        Task<DateTime> SetCenterSubscriptionAsync(SetCenterSubscriptionRequest request, CancellationToken ct = default);

        /// <summary>Center owner: per-teacher revenue report (collected / center cut / teacher cut) + grand totals.</summary>
        Task<CenterFinancialsDto> GetCenterFinancialsAsync(string tenantId, string ownerUserId, CancellationToken ct = default);

        /// <summary>Center owner: sets the center's revenue cut (%) for a teacher member. Null clears it.</summary>
        Task<string> SetTeacherShareAsync(string tenantId, string ownerUserId, string teacherUserId, decimal? sharePercent, CancellationToken ct = default);

        /// <summary>Center owner: one teacher's financial detail — totals + per-group breakdown (for the drill-in screen + statement).</summary>
        Task<CenterTeacherDetailDto> GetCenterTeacherDetailAsync(string tenantId, string ownerUserId, string teacherUserId, CancellationToken ct = default);

        /// <summary>Center owner: creates a staff (employee) account in the center with the given capability flags.</summary>
        Task<string> CreateStaffAsync(string tenantId, string ownerUserId, RegisterCenterStaffRequest request, CancellationToken ct = default);

        /// <summary>Center owner: updates a member's capability flags (used for staff/assistants).</summary>
        Task<string> SetMemberPermissionsAsync(string tenantId, string ownerUserId, string memberUserId, int permissions, CancellationToken ct = default);
    }
}
