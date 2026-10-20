using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour, ICameraController
{
    [Header("Settings")]
    [Tooltip("Space kept above the board (below the top HUD, room for the inlet counters) and on the sides (world units)")]
    public float edgeMargin = 1f;
    public float cameraZDepth = -10f;
    [Tooltip("The view is never narrower than this (world units), so the 7-slot tray always fits, even on 5-cell boards. It also keeps items the same size on screen in every level.")]
    public float minVisibleWidth = 7f;
    [Tooltip("Where the board sits when the screen has spare height: 0 = right above the tray, 0.5 = centred, 1 = at the top")]
    [Range(0f, 1f)] public float boardVerticalAlign = 0.25f;

    [Header("Screen Insets (optional, must implement IScreenInset)")]
    [Tooltip("Strip at the bottom of the screen (BottomBand). Board and tray are framed above it.")]
    [FormerlySerializedAs("bottomBand")]
    [SerializeField] private MonoBehaviour bottomInsetSource;
    [Tooltip("Bar at the top of the screen (TopHudInset). The board is framed below it.")]
    [SerializeField] private MonoBehaviour topInsetSource;

    private Camera cam;
    private IScreenInset _bottomInset;
    private IScreenInset _topInset;

    /// <summary>Injects the insets directly (tests, or code-built scenes). Overrides the inspector sources.</summary>
    public void Initialize(IScreenInset bottomInset, IScreenInset topInset)
    {
        _bottomInset = bottomInset;
        _topInset = topInset;
    }

    private void Awake()
    {
        EnsureCameraReference();
        ResolveInsets();
    }

    private void OnValidate()
    {
        if (bottomInsetSource != null && !(bottomInsetSource is IScreenInset))
        {
            Debug.LogWarning($"[CameraController] {bottomInsetSource.name} does not implement IScreenInset.", this);
            bottomInsetSource = null;
        }
        if (topInsetSource != null && !(topInsetSource is IScreenInset))
        {
            Debug.LogWarning($"[CameraController] {topInsetSource.name} does not implement IScreenInset.", this);
            topInsetSource = null;
        }
    }

    private void ResolveInsets()
    {
        _bottomInset ??= bottomInsetSource as IScreenInset;
        _topInset ??= topInsetSource as IScreenInset;
    }

    private float BottomFraction => _bottomInset != null ? _bottomInset.ScreenFraction : 0f;
    private float TopFraction => _topInset != null ? _topInset.ScreenFraction : 0f;

    /// <summary>
    /// Adjusts the camera to ensure the entire board is visible with a margin
    /// </summary>
    public void AdjustToBoard(int width, int height, float traySpace)
    {
        EnsureCameraReference();

        ResolveInsets();
        Frame(width, height, traySpace, BottomFraction, TopFraction, out float size, out float y);

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
        Frame(width, height, traySpace, reservedBottom, 0f, out size, out y);
    }

    /// <summary>Same as above, also keeping reservedTop (fraction of the screen height) free for the top HUD.</summary>
    public void Frame(int width, int height, float traySpace, float reservedBottom, float reservedTop, out float size, out float y)
    {
        EnsureCameraReference();
        float free = Mathf.Clamp(1f - reservedBottom - reservedTop, 0.1f, 1f);

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

    public float GetPlayAreaBottomY()
    {
        EnsureCameraReference();
        ResolveInsets();
        return GetCameraBottomY() + BottomFraction * cam.orthographicSize * 2f;
    }

    private void EnsureCameraReference()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }
}
