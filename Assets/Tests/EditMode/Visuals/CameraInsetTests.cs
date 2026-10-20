using NUnit.Framework;
using UnityEngine;

/// <summary>The camera frames the board through IScreenInset, without knowing which UI takes the space.</summary>
public class CameraInsetTests
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
        _camera.aspect = 2f;   // Wide view: the height decides the size
        _cameraController = _camObject.AddComponent<CameraController>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_camObject);
    }

    [Test]
    public void AdjustToBoard_WithInjectedInsets_FramesTheBoardBetweenThem()
    {
        _cameraController.Initialize(new FakeScreenInset { ScreenFraction = 0.16f },
                                     new FakeScreenInset { ScreenFraction = 0.12f });

        _cameraController.AdjustToBoard(7, 7, 2f);

        float size = _camera.orthographicSize;
        float viewTop = _camObject.transform.position.y + size;
        Assert.AreEqual(-3.5f - 2f, _cameraController.GetPlayAreaBottomY(), 1e-4f);   // Band top = board bottom - traySpace
        Assert.AreEqual(3.5f + 1.05f, viewTop - 0.12f * size * 2f, 1e-4f);           // HUD bottom = board top + paddings
    }

    [Test]
    public void PlayAreaBottom_WithoutInsets_IsTheCameraBottom()
    {
        _cameraController.AdjustToBoard(7, 7, 2f);

        Assert.AreEqual(_cameraController.GetCameraBottomY(), _cameraController.GetPlayAreaBottomY(), 1e-5f);
    }
}
