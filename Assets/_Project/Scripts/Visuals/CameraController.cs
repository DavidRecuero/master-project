using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Frames the level for any screen. Items keep the same size on the board and in the tray, so the zoom is
/// limited by whichever is wider (the board or the 7-slot tray) with thin side margins, and by the height
/// available between the top HUD and the tray. The board panel then fills all the free space (BoardFrame).
/// </summary>
public class CameraController : MonoBehaviour, ICameraController
{
    /// <summary>Result of framing a level: camera size and Y, plus the board panel face rect (world units).</summary>
    public struct Framing
    {
        public float size;
        public float y;
        public Rect boardFrame;
    }

    [Header("Width")]
    [Tooltip("Screen edge to the board panel and to the tray (world units). 0.15 = ~8 pt")]
    public float sideMargin = 0.15f;
    [Tooltip("Board cells to the panel edge, left and right (world units)")]
    public float boardSidePadding = 0.15f;
    [Tooltip("Width of the tray face (world units). The view is never narrower than this plus the side margins, so the 7 slots always fit at the same scale as the board.")]
    public float minVisibleWidth = 6.9f;

    [Header("Height")]
    [Tooltip("Top HUD bottom edge to the board panel (world units)")]
    public float topGap = 0.2f;
    [Tooltip("Panel top to the first row of cells: room for the inlet counters (world units)")]
    public float boardTopPadding = 0.85f;
    [Tooltip("Last row of cells to the panel's bottom edge (world units)")]
    public float boardBottomPadding = 0.12f;
    [Tooltip("Where the board sits inside the panel when there is spare height: 0 = bottom, 0.5 = centred, 1 = top")]
    [Range(0f, 1f)] public float boardVerticalAlign = 0.5f;

    public float cameraZDepth = -10f;

    [Header("Screen Insets (optional, must implement IScreenInset)")]
    [Tooltip("Strip at the bottom of the screen (BottomBand). Board and tray are framed above it.")]
    [FormerlySerializedAs("bottomBand")]
    [SerializeField] private MonoBehaviour bottomInsetSource;
    [Tooltip("Bar at the top of the screen (TopHudInset). The board is framed below it.")]
    [SerializeField] private MonoBehaviour topInsetSource;

    private Camera cam;
    private IScreenInset _bottomInset;
    private IScreenInset _topInset;
    private Rect? _boardFrame;

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
    /// Frames a board centred at the origin. traySpace = space between the top of the bottom inset
    /// and the board's last row (tray + gaps + panel bottom).
    /// </summary>
    public void AdjustToBoard(int width, int height, float traySpace)
    {
        EnsureCameraReference();
        ResolveInsets();

        Framing framing = Frame(width, height, traySpace, BottomFraction, TopFraction);

        cam.orthographicSize = framing.size;
        transform.position = new Vector3(0, framing.y, cameraZDepth);
        _boardFrame = framing.boardFrame;
    }

    /// <summary>
    /// Pure framing maths. reservedBottom / reservedTop = fraction of the screen height taken by the insets.
    /// Bottom to top: inset, tray area (traySpace), board, boardTopPadding + topGap, inset.
    /// </summary>
    public Framing Frame(int width, int height, float traySpace, float reservedBottom, float reservedTop)
    {
        EnsureCameraReference();
        float aspect = cam.aspect;
        float free = Mathf.Clamp(1f - reservedBottom - reservedTop, 0.1f, 1f);

        // Width: the board with its panel padding, or the tray, whichever is wider
        float viewWidth = Mathf.Max(width + 2f * boardSidePadding, minVisibleWidth) + 2f * sideMargin;
        float sizeForWidth = viewWidth / 2f / aspect;

        // Height: everything between the two insets
        float needed = topGap + boardTopPadding + height + traySpace;
        float sizeForHeight = needed / 2f / free;

        float size = Mathf.Max(sizeForWidth, sizeForHeight);

        float freeSpan = 2f * size * free;
        float spare = freeSpan - needed;
        float insetTop = -height / 2f - traySpace - spare * Mathf.Clamp01(boardVerticalAlign);

        // The panel fills the free area: full width minus margins, from below the HUD down to the tray area
        float halfWidth = size * aspect - sideMargin;
        float frameTop = insetTop + freeSpan - topGap;
        float frameBottom = insetTop + traySpace - boardBottomPadding;

        return new Framing
        {
            size = size,
            y = insetTop + size * (1f - 2f * reservedBottom),
            boardFrame = Rect.MinMaxRect(-halfWidth, frameBottom, halfWidth, frameTop)
        };
    }

    public bool TryGetBoardFrame(out Rect frame)
    {
        frame = _boardFrame ?? default;
        return _boardFrame.HasValue;
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
