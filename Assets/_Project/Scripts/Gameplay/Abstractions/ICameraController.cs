public interface ICameraController
{
    void AdjustToBoard(int width, int height, float traySpace);
    float GetCameraBottomY();

    /// <summary>World Y where the free play area starts: the top of the bottom inset, or the camera bottom without one.</summary>
    float GetPlayAreaBottomY();
}
