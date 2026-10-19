using NUnit.Framework;
using UnityEngine;

/// <summary>Tray panel sizing: it must wrap every slot with the same padding as the mockup.</summary>
public class TrayVisualsTests
{
    private GameObject _go;
    private TrayManager _tray;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Tray");
        _tray = _go.AddComponent<TrayManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void PanelFace_WrapsSevenSlots_WithItsPadding()
    {
        Vector2 face = _tray.PanelFaceSize();

        // 6 gaps between centres + one slot + padding on both sides
        float expectedWidth = 6 * 0.944f + 0.870f + 2 * 0.185f;
        float expectedHeight = 0.870f + 2 * 0.148f;

        Assert.AreEqual(expectedWidth, face.x, 1e-3f);
        Assert.AreEqual(expectedHeight, face.y, 1e-3f);
    }

    [Test]
    public void PanelFace_FollowsTheCapacity()
    {
        _tray.maxCapacity = 5;

        Assert.AreEqual(4 * 0.944f + 0.870f + 2 * 0.185f, _tray.PanelFaceSize().x, 1e-3f);
    }
}
