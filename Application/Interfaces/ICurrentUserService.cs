namespace Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? TenantId { get; }
        bool IsGlobalUser { get; }
    }
}