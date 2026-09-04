using UnityEngine;

public class UnityCameraProvider : ICameraProvider
{
    private Camera _camera;

    public UnityCameraProvider(Camera camera)
    {
        _camera = camera;
    }

    public Vector2 ScreenToWorldPoint(Vector2 screenPosition)
    {
        return _camera != null ? _camera.ScreenToWorldPoint(screenPosition) : Vector2.zero;
    }
}