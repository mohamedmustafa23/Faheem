namespace Application.Interfaces
{
    public interface ILinkService
    {
        Task<string> RequestLinkAsync(string parentId, string studentPhone, CancellationToken ct = default);
        Task<string> RespondToLinkAsync(string studentId, Guid linkId, bool accept, CancellationToken ct = default);
        Task<string> UnlinkChildAsync(string parentId, string studentId, CancellationToken ct = default);

        /// <summary>
        /// Student-initiated unlink — mirrors UnlinkChildAsync but the caller's
        /// identity is the student so we can hand the row to whichever side asks.
        /// </summary>
        Task<string> UnlinkParentAsync(string studentId, string parentId, CancellationToken ct = default);
    }
}