using Application.Interfaces;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Real Firebase Cloud Messaging implementation.
    /// Registered automatically when Firebase:CredentialsPath is configured in appsettings.
    /// Falls back to MockPushNotificationService if credentials are missing.
    /// </summary>
    public class FirebasePushNotificationService : IPushNotificationService
    {
        private readonly ILogger<FirebasePushNotificationService> _logger;

        public FirebasePushNotificationService(ILogger<FirebasePushNotificationService> logger)
        {
            _logger = logger;
        }

        public async Task SendAsync(
            IEnumerable<string> fcmTokens,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken ct = default)
        {
            var tokens = fcmTokens
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            if (tokens.Count == 0)
            {
                _logger.LogDebug("[FCM] SendAsync called with no valid tokens — skipping.");
                return;
            }

            // FCM requires data values to be strings. Copy into a fresh
            // dictionary so the caller can't mutate the payload mid-send.
            Dictionary<string, string>? dataPayload = null;
            if (data != null && data.Count > 0)
            {
                dataPayload = new Dictionary<string, string>(data.Count);
                foreach (var kv in data)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Value))
                        dataPayload[kv.Key] = kv.Value;
                }
                if (dataPayload.Count == 0) dataPayload = null;
            }

            // FCM multicast supports max 500 tokens per request
            const int batchSize = 500;

            for (int i = 0; i < tokens.Count; i += batchSize)
            {
                var batch = tokens.Skip(i).Take(batchSize).ToList();

                var message = new MulticastMessage
                {
                    Tokens = batch,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body,
                    },
                    Data = dataPayload,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "faheem_notifications",
                            Icon = "ic_notification",
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = "default",
                            Badge = 1,
                            ContentAvailable = true,
                        }
                    }
                };

                try
                {
                    var response = await FirebaseMessaging.DefaultInstance
                        .SendEachForMulticastAsync(message, ct);

                    _logger.LogInformation(
                        "[FCM] Batch {BatchIndex}: sent to {Total} token(s). ✓ {Success} succeeded, ✗ {Failure} failed.",
                        i / batchSize + 1, batch.Count, response.SuccessCount, response.FailureCount);

                    // Log individual failures for debugging
                    if (response.FailureCount > 0)
                    {
                        for (int j = 0; j < response.Responses.Count; j++)
                        {
                            var r = response.Responses[j];
                            if (!r.IsSuccess)
                            {
                                _logger.LogWarning(
                                    "[FCM] Token[{Index}] failed — ErrorCode: {Code}, Message: {Msg}",
                                    j,
                                    r.Exception?.MessagingErrorCode.ToString() ?? "Unknown",
                                    r.Exception?.Message ?? "—");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[FCM] Unexpected error while sending batch {BatchIndex}.", i / batchSize + 1);
                    // Don't rethrow — notification failure should never crash business logic
                }
            }
        }
    }
}
