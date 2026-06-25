namespace Domain.Enums
{
    /// <summary>
    /// A user's role within a single workspace (tenant). A user can hold a
    /// different role in each workspace they belong to.
    /// <para><see cref="Owner"/> — owns the workspace; manages members and the subscription
    /// (the individual teacher, or the center owner).</para>
    /// <para><see cref="Teacher"/> — a teacher operating inside a center; sees only their own groups.</para>
    /// <para><see cref="Assistant"/> — assists a teacher/center with limited permissions.</para>
    /// <para><see cref="Staff"/> — a center employee (secretary/manager) whose capabilities
    /// are exactly what the owner grants via the membership permission flags; owns no groups
    /// but operates across the whole center.</para>
    /// </summary>
    public enum WorkspaceRole
    {
        Owner = 0,
        Teacher = 1,
        Assistant = 2,
        Staff = 3
    }
}
