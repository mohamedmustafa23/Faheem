namespace Domain.Enums
{
    /// <summary>
    /// Lifecycle state of a workspace membership.
    /// <para><see cref="Active"/> — the user is a full member and can select this workspace.</para>
    /// <para><see cref="Invited"/> — invited by a center owner but has not accepted yet.</para>
    /// </summary>
    public enum WorkspaceMemberStatus
    {
        Active = 0,
        Invited = 1
    }
}
