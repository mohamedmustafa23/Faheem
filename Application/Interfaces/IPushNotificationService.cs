namespace Application.Interfaces
{
    public interface IPushNotificationService
    {
        /// <summary>
        /// Sends a push to every supplied FCM token.
        /// </summary>
        /// <param name="data">
        /// Optional key/value payload delivered in FCM's data field. The mobile
        /// client reads <c>data.route</c> to deep-link on tap. All FCM data
        /// values must be strings — caller is responsible for serialization.
        /// </param>
        Task SendAsync(
            IEnumerable<string> fcmTokens,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken ct = default);
    }
}
