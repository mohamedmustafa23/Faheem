using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Tenancy
{
    // Fires subscription renewal reminders once a day. NOTE: on free hosting the app
    // may sleep while idle, so this in-process timer only runs while the app is awake.
    // For guaranteed delivery later, expose SendDueRemindersAsync behind a token-gated
    // endpoint and drive it from an external cron (e.g. cron-job.org).
    public class SubscriptionReminderService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionReminderService> _logger;

        public SubscriptionReminderService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var subs = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                    var count = await subs.SendDueRemindersAsync(stoppingToken);
                    if (count > 0) _logger.LogInformation("Subscription reminders sent: {Count}", count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Subscription reminder run failed");
                }

                try { await Task.Delay(Interval, stoppingToken); }
                catch (TaskCanceledException) { break; }
            }
        }
    }
}
