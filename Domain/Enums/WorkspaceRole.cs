namespace Domain.Enums
{
    /// <summary>
    /// A user's role within a single workspace (tenant). A user can hold a
    /// different role in each workspace they belong to.
    /// <para><see cref="Owner"/> — owns the workspace; manages members and the subscription
    /// (the individual teacher, or the center owner).</para>
    /// <para><see cref="Teacher"/> — a teacher operating inside a center; sees only their own groups.</para>
    /// <para><see cref="Assistant"/> — assists a teacher/center with limited permissions.</para>
    /// </summary>
    public enum WorkspaceRole
    {
        Owner = 0,
        Teacher = 1,
        Assistant = 2
    }
}
