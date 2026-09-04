using UnityEngine;

public class CameraController : MonoBehaviour, ICameraController
{
    [Header("Settings")]
    public float edgeMargin = 1f;   // Extra space around the board
    public float cameraZDepth = -10f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    /// <summary>
    /// Adjusts the camera to ensure the entire board is visible with a margin
    /// </summary>
    public void AdjustToBoard(int width, int height, float traySpace)
    {
        if (cam == null) cam = Camera.main;

        // Extra space reserved at the bottom for the tray
        float totalHeight = height + traySpace;

        transform.position = new Vector3(0, -traySpace / 2f, cameraZDepth);

        float sizeForHeight = totalHeight / 2f;
        float sizeForWidth = (width / 2f) / cam.aspect;

        //Pick the larger size(so nothing gets cut off) and add the margin
        cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth) + edgeMargin;
    }

    public float GetCameraBottomY()
    {
        Camera cam = GetComponent<Camera>();
        return cam.transform.position.y - cam.orthographicSize;
    }
}