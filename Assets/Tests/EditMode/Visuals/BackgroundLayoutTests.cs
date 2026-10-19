using NUnit.Framework;
using UnityEngine;

/// <summary>Background tile cover and booster band sizing (pure maths, no screen needed).</summary>
public class BackgroundLayoutTests
{
    [Test]
    public void CoverSize_CoversTheViewPlusOverscan()
    {
        Vector2 size = BackgroundView.CoverSize(5f, 0.5f, 1f);

        Assert.AreEqual(7f, size.x, 1e-4f);    // 10 * 0.5 + 2
        Assert.AreEqual(12f, size.y, 1e-4f);   // 10 + 2
    }

    [Test]
    public void BandHeight_iPhone13_MatchesTheMockup()
    {
        // 1170 x 2532 = 3 px per pt; 101 pt band + 34 pt home indicator = 135 pt
        float px = BottomBand.HeightPixels(1170f, 2532f, 102f, 101f, 390f, 844f);

        Assert.AreEqual(135f * 3f, px, 1e-3f);
    }

    [Test]
    public void BandHeight_WideScreen_DoesNotGrowWithTheWidth()
    {
        // Tablet 1536 x 2048: the height limits the scale (2048 / 844), not the width (1536 / 390)
        float ppp = BottomBand.PixelsPerPoint(1536f, 2048f, 390f, 844f);

        Assert.AreEqual(2048f / 844f, ppp, 1e-4f);
    }

    [Test]
    public void TraySitsOverTheBand_WithItsGapAndShadow()
    {
        var go = new GameObject("Tray");
        TrayManager tray = go.AddComponent<TrayManager>();

        float y = tray.CenterYAboveBand(-5f);

        Assert.AreEqual(-5f + 0.2f + 0.111f + (0.870f + 0.296f) / 2f, y, 1e-3f);
        Object.DestroyImmediate(go);
    }
}
