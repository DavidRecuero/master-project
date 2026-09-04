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
    public void AdjustToBoard_SetsCorrectPositionAndOrthographicSize()
    {
        int width = 10;
        int height = 10;
        float traySpace = 2.5f;

        _cameraController.AdjustToBoard(width, height, traySpace);

        Assert.AreEqual(0f, _camObject.transform.position.x);
        Assert.AreEqual(-1.25f, _camObject.transform.position.y);
        Assert.AreEqual(-10f, _camObject.transform.position.z);
        Assert.Greater(_camera.orthographicSize, 0f);
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