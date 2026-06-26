namespace Domain.Enums
{
    /// <summary>
    /// The kind of workspace a tenant represents.
    /// <para><see cref="Individual"/> — a single teacher's private workspace (the teacher pays).</para>
    /// <para><see cref="Center"/> — a tutoring center that hosts multiple teachers (the center pays).</para>
    /// </summary>
    public enum TenantType
    {
        Individual = 0,
        Center = 1
    }
}
