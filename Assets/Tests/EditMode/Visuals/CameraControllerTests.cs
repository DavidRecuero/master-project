using NUnit.Framework;
using UnityEngine;

public class CameraControllerTests
{
    private GameObject _camObject;
    private Camera _camera;
    private CameraController _cameraController;

    [SetUp]
    public void SetUp()
    {
        _camObject = new GameObject("MainCamera");
        _camera = _camObject.AddComponent<Camera>();
        _camera.orthographic = true;
        _cameraController = _camObject.AddComponent<CameraController>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_camObject);
    }

    [Test]
    public void AdjustToBoard_WithoutBand_LeavesTheMarginOnTopAndTheTraySpaceBelow()
    {
        _camera.aspect = 2f;   // Wide view: the height decides the size

        _cameraController.AdjustToBoard(10, 10, 2.5f);

        float top = _camObject.transform.position.y + _camera.orthographicSize;
        float bottom = _camObject.transform.position.y - _camera.orthographicSize;

        Assert.AreEqual(0f, _camObject.transform.position.x);
        Assert.AreEqual(-10f, _camObject.transform.position.z);
        Assert.AreEqual(5f + 0.2f + 0.85f, top, 1e-4f);  // Board top + boardTopPadding + topGap
        Assert.AreEqual(-5f - 2.5f, bottom, 1e-4f);      // Board bottom - traySpace
    }

    [Test]
    public void Frame_WithBand_PutsTheTrayAreaRightOnTheBand()
    {
        _camera.aspect = 2f;
        const float band = 0.16f;

        CameraController.Framing f = _cameraController.Frame(7, 7, 2f, band, 0f);

        float bandTop = (f.y - f.size) + band * f.size * 2f;
        Assert.AreEqual(-3.5f - 2f, bandTop, 1e-4f);
        Assert.AreEqual(3.5f + 1.05f, f.y + f.size, 1e-4f);
    }

    [Test]
    public void Frame_TallScreen_KeepsEverythingAboveTheBandAndInsideTheView()
    {
        _camera.aspect = 1170f / 2532f;   // iPhone 13
        const float band = 0.16f;

        CameraController.Framing f = _cameraController.Frame(7, 7, 2f, band, 0f);

        float bandTop = (f.y - f.size) + band * f.size * 2f;
        Assert.LessOrEqual(bandTop, -3.5f - 2f + 1e-4f);                      // Tray area fits over the band
        Assert.GreaterOrEqual(f.y + f.size, 3.5f + 1.05f - 1e-4f);            // Top room kept
        Assert.GreaterOrEqual(f.size * 2f * _camera.aspect, 7f + 0.6f - 1e-4f); // Board + paddings + margins fit across
    }

    [Test]
    public void Frame_NarrowBoardOnTallScreen_ZoomsToTheTrayWidth()
    {
        _camera.aspect = 1170f / 2532f;   // iPhone 13

        CameraController.Framing f = _cameraController.Frame(5, 5, 2f, 0.16f, 0.12f);

        // Width-limited: the view is exactly the tray plus the side margins
        Assert.AreEqual(6.9f + 0.3f, f.size * 2f * _camera.aspect, 1e-4f);
    }

    [Test]
    public void Frame_BoardFrame_FillsTheFreeArea()
    {
        _camera.aspect = 1170f / 2532f;
        const float band = 0.16f, hud = 0.12f;

        CameraController.Framing f = _cameraController.Frame(5, 5, 2f, band, hud);

        float viewTop = f.y + f.size, viewBottom = f.y - f.size;
        float bandTop = viewBottom + band * f.size * 2f;
        float hudBottom = viewTop - hud * f.size * 2f;

        Assert.AreEqual(f.size * _camera.aspect - 0.15f, f.boardFrame.xMax, 1e-4f);   // Side margin
        Assert.AreEqual(-f.boardFrame.xMax, f.boardFrame.xMin, 1e-4f);
        Assert.AreEqual(hudBottom - 0.2f, f.boardFrame.yMax, 1e-4f);                 // Just below the HUD
        Assert.AreEqual(bandTop + 2f - 0.12f, f.boardFrame.yMin, 1e-4f);             // Just above the tray area
        Assert.GreaterOrEqual(f.boardFrame.yMax, 2.5f + 0.85f - 1e-4f);              // Board and counters inside
        Assert.LessOrEqual(f.boardFrame.yMin, -2.5f - 0.12f + 1e-4f);
    }

    [Test]
    public void AdjustToBoard_StoresTheBoardFrame()
    {
        Assert.IsFalse(_cameraController.TryGetBoardFrame(out _));

        _cameraController.AdjustToBoard(5, 5, 2f);

        Assert.IsTrue(_cameraController.TryGetBoardFrame(out Rect frame));
        Assert.Greater(frame.width, 5f);
    }

    [Test]
    public void GetCameraBottomY_ReturnsCorrectCoordinate()
    {
        _camObject.transform.position = new Vector3(0, 0, -10);
        _camera.orthographicSize = 5f;

        float bottomY = _cameraController.GetCameraBottomY();

        Assert.AreEqual(-5f, bottomY);
    }
}
