using UnityEngine;

public class CameraController : MonoBehaviour, ICameraController
{
    [Header("Settings")]
    [Tooltip("Space kept above the board (top HUD) and on the sides (world units)")]
    public float edgeMargin = 1f;
    public float cameraZDepth = -10f;
    [Tooltip("The view is never narrower than this (world units), so the 7-slot tray always fits, even on 5-cell boards. It also keeps items the same size on screen in every level.")]
    public float minVisibleWidth = 7f;
    [Tooltip("Where the board sits when the screen has spare height: 0 = right above the tray, 0.5 = centred, 1 = at the top")]
    [Range(0f, 1f)] public float boardVerticalAlign = 0.25f;

    [Header("Bottom Band")]
    [Tooltip("Optional. Board and tray are framed in the part of the screen above the booster band.")]
    [SerializeField] private BottomBand bottomBand;

    private Camera cam;

    private void Awake()
    {
        EnsureCameraReference();
    }

    /// <summary>
    /// Adjusts the camera to ensure the entire board is visible with a margin
    /// </summary>
    public void AdjustToBoard(int width, int height, float traySpace)
    {
        EnsureCameraReference();

        float reserved = bottomBand != null ? bottomBand.ScreenFraction : 0f;
        Frame(width, height, traySpace, reserved, out float size, out float y);

        cam.orthographicSize = size;
        transform.position = new Vector3(0, y, cameraZDepth);
    }

    /// <summary>
    /// Orthographic size and camera Y for a board centred at the origin.
    /// The view is split, bottom to top, into: the band (reservedBottom, fraction of the screen height),
    /// the tray area (traySpace world units, the tray sits right on the band), the board, and edgeMargin on top.
    /// Spare height (tall screens) is shared above/below the board following boardVerticalAlign.
    /// </summary>
    public void Frame(int width, int height, float traySpace, float reservedBottom, out float size, out float y)
    {
        EnsureCameraReference();
        float free = Mathf.Clamp(1f - reservedBottom, 0.1f, 1f);

        float sizeForHeight = (height + traySpace + edgeMargin) / 2f / free;
        float sizeForWidth = (Mathf.Max(width, minVisibleWidth) / 2f) / cam.aspect + edgeMargin;

        // Pick the larger size so nothing gets cut off
        size = Mathf.Max(sizeForHeight, sizeForWidth);

        float spare = 2f * size * free - edgeMargin - traySpace - height;
        float bandTop = -height / 2f - traySpace - spare * Mathf.Clamp01(boardVerticalAlign);
        y = bandTop + size * (1f - 2f * reservedBottom);
    }

    public float GetCameraBottomY()
    {
        EnsureCameraReference();
        return cam.transform.position.y - cam.orthographicSize;
    }

    private void EnsureCameraReference()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }
}
