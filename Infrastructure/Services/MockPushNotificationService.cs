using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    // Swap this registration in Startup.cs for a real FirebasePushNotificationService when ready
    public class MockPushNotificationService : IPushNotificationService
    {
        private readonly ILogger<MockPushNotificationService> _logger;

        public MockPushNotificationService(ILogger<MockPushNotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(
            IEnumerable<string> fcmTokens,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken ct = default)
        {
            var tokens = fcmTokens.ToList();
            var route = data != null && data.TryGetValue("route", out var r) ? r : "—";
            _logger.LogInformation(
                "[PUSH MOCK] Sending to {Count} device(s). Title: {Title}. Route: {Route}",
                tokens.Count, title, route);
            return Task.CompletedTask;
        }
    }
}
