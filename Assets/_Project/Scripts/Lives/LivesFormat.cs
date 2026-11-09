using System;
using System.Globalization;

/// <summary>Countdown text of the lives ("29:59", "1:05:00"). Digits only, the same in every language.</summary>
public static class LivesFormat
{
    /// <summary>
    /// mm:ss, or h:mm:ss from one hour. Rounded up to the second, so it never shows 00:00 while
    /// there is still time left (the life comes the moment the text would reach 00:00).
    /// </summary>
    public static string Countdown(TimeSpan time)
    {
        long seconds = (long)Math.Ceiling(Math.Max(0, time.TotalSeconds));
        long hours = seconds / 3600;
        long minutes = seconds % 3600 / 60;
        long secs = seconds % 60;

        return hours > 0
            ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", hours, minutes, secs)
            : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes, secs);
    }
}
