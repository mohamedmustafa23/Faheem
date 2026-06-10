namespace Application.Interfaces
{
    /// <summary>
    /// Single source of truth for "now". Timestamps are persisted in UTC, but any
    /// business-day logic ("what day is it", overdue checks, next-occurrence dates)
    /// must use <see cref="TodayInAppZone"/> — the raw UTC date drifts by a day
    /// around midnight relative to the user's local (Egyptian) day.
    /// Injecting this instead of calling DateTime.UtcNow directly also keeps the
    /// logic unit-testable.
    /// </summary>
    public interface IDateTimeService
    {
        /// <summary>Current UTC instant — use for stored timestamps.</summary>
        DateTime UtcNow { get; }

        /// <summary>Current wall-clock time in the app's business zone (Egypt).</summary>
        DateTime NowInAppZone { get; }

        /// <summary>Today's calendar date in the app's business zone (Egypt).</summary>
        DateOnly TodayInAppZone { get; }
    }
}
