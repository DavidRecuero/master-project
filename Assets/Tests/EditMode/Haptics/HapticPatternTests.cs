using System;
using NUnit.Framework;

public class HapticPatternTests
{
    [Test]
    public void EveryType_HasAValidPreset()
    {
        foreach (HapticType type in Enum.GetValues(typeof(HapticType)))
        {
            if (type == HapticType.None) continue;

            HapticPattern pattern = HapticPresets.Get(type);
            Assert.IsNotNull(pattern, $"No preset for {type}");
            Assert.IsTrue(pattern.IsValid, $"Preset of {type} is not valid");
        }
    }

    [Test]
    public void None_HasNoPattern()
    {
        Assert.IsNull(HapticPresets.Get(HapticType.None));
    }

    [Test]
    public void Impacts_GetStrongerFromLightToHeavy()
    {
        int light = HapticPresets.Get(HapticType.LightImpact).Amplitudes[0];
        int medium = HapticPresets.Get(HapticType.MediumImpact).Amplitudes[0];
        int heavy = HapticPresets.Get(HapticType.HeavyImpact).Amplitudes[0];

        Assert.Less(light, medium);
        Assert.Less(medium, heavy);
    }

    [Test]
    public void Impacts_AlsoGetLongerFromLightToHeavy_ForMotorsWithoutAmplitudeControl()
    {
        long selection = HapticPresets.Get(HapticType.Selection).TotalMilliseconds;
        long light = HapticPresets.Get(HapticType.LightImpact).TotalMilliseconds;
        long medium = HapticPresets.Get(HapticType.MediumImpact).TotalMilliseconds;
        long heavy = HapticPresets.Get(HapticType.HeavyImpact).TotalMilliseconds;

        Assert.Less(selection, light);
        Assert.Less(light, medium);
        Assert.Less(medium, heavy);
    }

    [Test]
    public void Presets_StayShort()
    {
        foreach (HapticType type in Enum.GetValues(typeof(HapticType)))
        {
            HapticPattern pattern = HapticPresets.Get(type);
            if (pattern == null) continue;
            Assert.LessOrEqual(pattern.TotalMilliseconds, 500, $"{type} is too long for game feedback");
        }
    }

    [Test]
    public void IsValid_FalseWhenLengthsDiffer()
    {
        Assert.IsFalse(new HapticPattern(new long[] { 10, 20 }, new[] { 100 }).IsValid);
    }

    [Test]
    public void IsValid_FalseWhenAmplitudeOutOfRange()
    {
        Assert.IsFalse(HapticPattern.OneShot(10, 300).IsValid);
        Assert.IsFalse(HapticPattern.OneShot(10, -5).IsValid);
    }

    [Test]
    public void IsValid_FalseWhenOnlyPauses()
    {
        Assert.IsFalse(new HapticPattern(new long[] { 10, 20 }, new[] { 0, 0 }).IsValid);
    }

    [Test]
    public void TotalMilliseconds_CountsPausesToo()
    {
        var pattern = new HapticPattern(new long[] { 30, 90, 50 }, new[] { 120, 0, 255 });
        Assert.AreEqual(170, pattern.TotalMilliseconds);
    }

    [Test]
    public void ToOnOffTimings_StartsWithAnOffAndAlternates()
    {
        var pattern = new HapticPattern(new long[] { 30, 90, 50 }, new[] { 120, 0, 255 });
        CollectionAssert.AreEqual(new long[] { 0, 30, 90, 50 }, pattern.ToOnOffTimings());
    }

    [Test]
    public void ToOnOffTimings_LeadingPauseBecomesTheFirstOff()
    {
        var pattern = new HapticPattern(new long[] { 20, 40 }, new[] { 0, 200 });
        CollectionAssert.AreEqual(new long[] { 20, 40 }, pattern.ToOnOffTimings());
    }

    [Test]
    public void ToOnOffTimings_MergesSegmentsInTheSameState()
    {
        // Two strengths in a row are both "on" for a motor without amplitude control
        var pattern = new HapticPattern(new long[] { 20, 30, 40 }, new[] { 100, 255, 0 });
        CollectionAssert.AreEqual(new long[] { 0, 50, 40 }, pattern.ToOnOffTimings());
    }
}
