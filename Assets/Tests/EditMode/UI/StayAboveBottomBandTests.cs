using NUnit.Framework;

/// <summary>Menu content offset over the bottom band (pure maths, no screen needed).</summary>
public class StayAboveBottomBandTests
{
    [Test]
    public void BottomOffset_iPhone13_MatchesTheOldLayout()
    {
        // Band 405 px (101 pt + 34 pt home indicator, 3 px/pt), safe area 102 px -> 303 px = 101 pt
        Assert.AreEqual(303f, StayAboveBottomBand.BottomOffsetPixels(405f, 102f, 0f), 1e-4f);
    }

    [Test]
    public void BottomOffset_AndroidWithoutSafeArea_FollowsTheTallerBand()
    {
        // Band 375 px (101 pt + 24 pt minimum inset, 3 px/pt), safe area 0 -> the whole band
        Assert.AreEqual(375f, StayAboveBottomBand.BottomOffsetPixels(375f, 0f, 0f), 1e-4f);
    }

    [Test]
    public void BottomOffset_AddsTheGap()
    {
        Assert.AreEqual(333f, StayAboveBottomBand.BottomOffsetPixels(405f, 102f, 30f), 1e-4f);
    }

    [Test]
    public void BottomOffset_IsNeverNegative()
    {
        Assert.AreEqual(0f, StayAboveBottomBand.BottomOffsetPixels(50f, 102f, 0f), 1e-4f);
    }
}
