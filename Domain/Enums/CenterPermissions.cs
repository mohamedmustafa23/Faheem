namespace Domain.Enums
{
    /// <summary>
    /// Operational capabilities a center grants to one of its members, stored as a
    /// bitmask on <c>WorkspaceMember.Permissions</c>. These are merged into the JWT's
    /// permission claims at token-issue time (on top of the user's Identity-role
    /// permissions), so they only widen what a member can do inside that one center.
    /// <para>
    /// An individual teacher needs none of these — their toolkit comes from the
    /// global <c>Teacher</c> role and their data is scoped by the group query filter.
    /// The center <c>Owner</c> gets <see cref="All"/>; future Staff/assistant members
    /// get whatever subset the owner configures.
    /// </para>
    /// <para>
    /// Adding a capability later is one enum value + one mapping line — no migration
    /// (the column is a single int).
    /// </para>
    /// </summary>
    [System.Flags]
    public enum CenterPermissions
    {
        None = 0,
        ManageGroups = 1,
        ManageAttendance = 2,
        ManageStudents = 4,
        ManagePayments = 8,
        ViewFinancials = 16,
        DeleteGroups = 32,

        All = ManageGroups | ManageAttendance | ManageStudents | ManagePayments | ViewFinancials | DeleteGroups
    }
}
