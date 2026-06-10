using Application.Interfaces;

namespace Infrastructure.Services
{
    /// <inheritdoc cref="IDateTimeService"/>
    public class DateTimeService : IDateTimeService
    {
        // Egypt observes DST again since 2023, so a hardcoded +2 offset would be
        // wrong for part of the year. Resolve the IANA zone (works cross-platform
        // on .NET 6+ via ICU) and let TimeZoneInfo handle the DST transitions.
        private static readonly TimeZoneInfo AppZone = ResolveAppZone();

        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime NowInAppZone => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppZone);

        public DateOnly TodayInAppZone => DateOnly.FromDateTime(NowInAppZone);

        private static TimeZoneInfo ResolveAppZone()
        {
            // IANA id first (Linux/containers + modern Windows via ICU), then the
            // legacy Windows id, then a fixed +2 fallback so the app never fails
            // to boot on a host missing the tz database.
            foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            return TimeZoneInfo.CreateCustomTimeZone("Egypt", TimeSpan.FromHours(2), "Egypt", "Egypt");
        }
    }
}
