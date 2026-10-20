using NUnit.Framework;
using UnityEngine;

/// <summary>HUD maths: coin formatting, safe area anchors and the camera space kept for the top bar.</summary>
public class HudLayoutTests
{
    [TestCase(0, ".", "0")]
    [TestCase(250, ".", "250")]
    [TestCase(1250, ".", "1.250")]
    [TestCase(1234567, ".", "1.234.567")]
    [TestCase(1250, "", "1250")]
    [TestCase(-5, ".", "0")]
    public void Coins_AreFormattedWithThousandsSeparator(int amount, string separator, string expected)
    {
        Assert.AreEqual(expected, CoinsView.Format(amount, separator));
    }

    [Test]
    public void SafeArea_iPhone13_LeavesTheNotchAndHomeIndicatorOut()
    {
        // 1170 x 2532, safe area from 102 px (34 pt) to 2391 px (47 pt from the top)
        var safe = new Rect(0f, 102f, 1170f, 2532f - 102f - 141f);

        SafeAreaFitter.Anchors(safe, 1170f, 2532f, out Vector2 min, out Vector2 max);

        Assert.AreEqual(0f, min.x, 1e-5f);
        Assert.AreEqual(102f / 2532f, min.y, 1e-5f);
        Assert.AreEqual(1f, max.x, 1e-5f);
        Assert.AreEqual(1f - 141f / 2532f, max.y, 1e-5f);
    }

    [Test]
    public void TopFraction_IsTheScreenPartAboveTheHud()
    {
        Assert.AreEqual(0.25f, TopHudInset.FractionAbove(750f, 1000f), 1e-5f);
        Assert.AreEqual(0f, TopHudInset.FractionAbove(1000f, 1000f), 1e-5f);
    }

    [Test]
    public void Frame_WithTopHud_KeepsTheBoardBelowIt()
    {
        var go = new GameObject("Cam");
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.aspect = 2f;   // Height decides
        CameraController controller = go.AddComponent<CameraController>();

        const float band = 0.16f, top = 0.12f;
        CameraController.Framing f = controller.Frame(7, 7, 2f, band, top);

        float viewTop = f.y + f.size;
        float hudBottom = viewTop - top * f.size * 2f;
        float bandTop = (f.y - f.size) + band * f.size * 2f;

        Assert.AreEqual(3.5f + 1.05f, hudBottom, 1e-4f); // Board top + boardTopPadding + topGap
        Assert.AreEqual(-3.5f - 2f, bandTop, 1e-4f);     // Board bottom - traySpace
        Object.DestroyImmediate(go);
    }
}
