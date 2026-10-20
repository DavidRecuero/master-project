using UnityEngine;

/// <summary>
/// Tells the camera how much of the screen the top HUD bar takes, measured from the top edge
/// down to this rect's bottom (safe area included). Put it on HUD_TopBar.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TopHudInset : MonoBehaviour, IScreenInset
{
    private RectTransform _rect;
    private readonly Vector3[] _corners = new Vector3[4];

    public float ScreenFraction
    {
        get
        {
            if (!isActiveAndEnabled) return 0f;
            _rect ??= GetComponent<RectTransform>();

            // Make sure the safe area and the layout are applied before reading the rect
            Canvas.ForceUpdateCanvases();
            _rect.GetWorldCorners(_corners);   // Overlay canvas: world corners are screen pixels
            return FractionAbove(_corners[0].y, Screen.height);
        }
    }

    /// <summary>Part of the screen height above a screen-space Y (pixels from the bottom).</summary>
    public static float FractionAbove(float bottomPixels, float screenHeight)
    {
        if (screenHeight <= 0f) return 0f;
        return Mathf.Clamp01((screenHeight - bottomPixels) / screenHeight);
    }
}
