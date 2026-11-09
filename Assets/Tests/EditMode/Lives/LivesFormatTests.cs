using System;
using NUnit.Framework;

public class LivesFormatTests
{
    [TestCase(0, "00:00")]
    [TestCase(59, "00:59")]
    [TestCase(60, "01:00")]
    [TestCase(1799, "29:59")]
    [TestCase(1800, "30:00")]
    [TestCase(3600, "1:00:00")]
    [TestCase(3905, "1:05:05")]
    public void Countdown_ShowsMinutesAndSeconds_AndHoursFromOneHour(int seconds, string expected)
    {
        Assert.AreEqual(expected, LivesFormat.Countdown(TimeSpan.FromSeconds(seconds)));
    }

    [Test]
    public void Countdown_RoundsUp_SoItNeverShowsZeroWithTimeLeft()
    {
        Assert.AreEqual("00:01", LivesFormat.Countdown(TimeSpan.FromMilliseconds(200)));
        Assert.AreEqual("30:00", LivesFormat.Countdown(TimeSpan.FromSeconds(1799.4)));
    }

    [Test]
    public void Countdown_NegativeTime_IsZero()
    {
        Assert.AreEqual("00:00", LivesFormat.Countdown(TimeSpan.FromSeconds(-5)));
    }
}
