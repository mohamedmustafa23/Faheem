using Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common
{
    public static class DbContextRetryExtensions
    {
        public const int DefaultMaxRetries = 3;

        /// <summary>
        /// Runs <paramref name="action"/> and retries on optimistic-concurrency conflicts
        /// (DbUpdateConcurrencyException). On each retry the change tracker is cleared so
        /// the next attempt re-reads fresh state. After <paramref name="maxRetries"/>
        /// attempts a 409 ConflictException is raised so the caller can surface a clean
        /// error to the client.
        /// </summary>
        public static async Task<T> ExecuteWithConcurrencyRetryAsync<T>(
            this DbContext context,
            Func<Task<T>> action,
            int maxRetries = DefaultMaxRetries)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxRetries)
                {
                    foreach (var entry in context.ChangeTracker.Entries().ToList())
                        entry.State = EntityState.Detached;
                }
            }

            throw new ConflictException([
                "Could not complete the operation due to a concurrent update. Please retry."
            ]);
        }
    }
}
