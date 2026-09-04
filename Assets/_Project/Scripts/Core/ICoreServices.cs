using UnityEngine;

// Interface to abstract user's level  and persistance
public interface IUserDataProvider
{
    int CurrentLevel { get; }
    void ResetData();
}

// Interface for scene loads
public interface ISceneLoader
{
    void LoadScene(int sceneIndex);
    void ReloadCurrentScene();
}

public interface ICameraProvider
{
    Vector2 ScreenToWorldPoint(Vector2 screenPosition);
}

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