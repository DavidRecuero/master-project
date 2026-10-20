using UnityEngine;

/// <summary>
/// Stretches this RectTransform over Screen.safeArea, so whatever sits inside it
/// never goes under the notch, the camera hole or the rounded corners.
/// Put it on a full-screen child of the Canvas and place the top HUD inside.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rect;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreen;

    private void OnEnable()
    {
        Apply(true);
    }

    private void Update()
    {
        Apply(false);
    }

    /// <summary>Normalised anchors (0..1) for a safe area inside a screen.</summary>
    public static void Anchors(Rect safeArea, float screenWidth, float screenHeight, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
        max = new Vector2(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);
    }

    private void Apply(bool force)
    {
        Rect safe = Screen.safeArea;
        var screen = new Vector2Int(Screen.width, Screen.height);
        if (!force && safe == _lastSafeArea && screen == _lastScreen) return;
        if (screen.x <= 0 || screen.y <= 0) return;

        _lastSafeArea = safe;
        _lastScreen = screen;

        _rect ??= GetComponent<RectTransform>();
        Anchors(safe, screen.x, screen.y, out Vector2 min, out Vector2 max);
        _rect.anchorMin = min;
        _rect.anchorMax = max;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }
}
