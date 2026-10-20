using NUnit.Framework;
using UnityEngine;

public class BoardPanelTests
{
    [Test]
    public void PanelFace_WithoutFrame_FitsTheBoard()
    {
        Rect fitted = Rect.MinMaxRect(-2.6f, -2.6f, 2.6f, 3.4f);

        Assert.AreEqual(fitted, PipeBoardRenderer.PanelFace(fitted, null));
    }

    [Test]
    public void PanelFace_WithFrame_FillsTheFrame()
    {
        Rect fitted = Rect.MinMaxRect(-2.6f, -2.6f, 2.6f, 3.4f);
        Rect frame = Rect.MinMaxRect(-3.45f, -3f, 3.45f, 5f);

        Assert.AreEqual(frame, PipeBoardRenderer.PanelFace(fitted, frame));
    }

    [Test]
    public void PanelFace_NeverCutsTheBoard()
    {
        Rect fitted = Rect.MinMaxRect(-3.6f, -3.6f, 3.6f, 4.4f);
        Rect frame = Rect.MinMaxRect(-3.45f, -3f, 3.45f, 5f);

        Rect face = PipeBoardRenderer.PanelFace(fitted, frame);

        Assert.AreEqual(-3.6f, face.xMin, 1e-5f);
        Assert.AreEqual(3.6f, face.xMax, 1e-5f);
        Assert.AreEqual(-3.6f, face.yMin, 1e-5f);
        Assert.AreEqual(5f, face.yMax, 1e-5f);
    }
}
