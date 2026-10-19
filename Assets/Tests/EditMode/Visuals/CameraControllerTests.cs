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
        Assert.AreEqual(5f + 1f, top, 1e-4f);            // Board top + edgeMargin
        Assert.AreEqual(-5f - 2.5f, bottom, 1e-4f);      // Board bottom - traySpace
    }

    [Test]
    public void Frame_WithBand_PutsTheTrayAreaRightOnTheBand()
    {
        _camera.aspect = 2f;
        const float band = 0.16f;

        _cameraController.Frame(7, 7, 2f, band, out float size, out float y);

        float bandTop = (y - size) + band * size * 2f;
        Assert.AreEqual(-3.5f - 2f, bandTop, 1e-4f);
        Assert.AreEqual(3.5f + 1f, y + size, 1e-4f);
    }

    [Test]
    public void Frame_TallScreen_KeepsEverythingAboveTheBandAndInsideTheView()
    {
        _camera.aspect = 1170f / 2532f;   // iPhone 13
        const float band = 0.16f;

        _cameraController.Frame(7, 7, 2f, band, out float size, out float y);

        float bandTop = (y - size) + band * size * 2f;
        Assert.LessOrEqual(bandTop, -3.5f - 2f + 1e-4f);                 // Tray area fits over the band
        Assert.GreaterOrEqual(y + size, 3.5f + 1f - 1e-4f);              // Top margin kept
        Assert.GreaterOrEqual(size * 2f * _camera.aspect, 7f - 1e-4f);   // 7 slots fit across
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
