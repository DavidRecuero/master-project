using UnityEngine;

/// <summary>
/// Keeps the bottom edge of this RectTransform right above the BottomBand, so the content never goes
/// under the boosters / nav buttons, whatever height the band ends up having on each device.
/// Put it on a stretched child of the SafeAreaFitter rect (e.g. MM_SafeArea/Content): the offset is
/// measured from the bottom of the safe area. Only the bottom offset is driven; the rest stays as set.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class StayAboveBottomBand : MonoBehaviour
{
    [Tooltip("Band this rect must stay above")]
    public BottomBand band;
    [Tooltip("Extra space between the band's top edge and this rect (pt, same units as BottomBand)")]
    public float gapPt = 0f;

    private RectTransform _rect;
    private Canvas _canvas;

    /// <summary>
    /// Bottom offset in screen pixels, from the bottom of the safe area: the part of the band that is
    /// above the safe area, plus the gap. Never negative.
    /// </summary>
    public static float BottomOffsetPixels(float bandHeightPixels, float safeBottomPixels, float gapPixels)
    {
        return Mathf.Max(0f, bandHeightPixels - safeBottomPixels + gapPixels);
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
        // Cheap; keeps up with rotation, editor resolution changes and safe area changes
        Apply();
    }

    public void Apply()
    {
        if (band == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        _rect ??= GetComponent<RectTransform>();
        _canvas ??= GetComponentInParent<Canvas>();
        float scale = _canvas != null && _canvas.rootCanvas.scaleFactor > 0f ? _canvas.rootCanvas.scaleFactor : 1f;

        float ppp = BottomBand.PixelsPerPoint(Screen.width, Screen.height, band.designWidthPt, band.designHeightPt);
        float offset = BottomOffsetPixels(band.HeightPixelsNow, Screen.safeArea.yMin, gapPt * ppp) / scale;

        Vector2 min = _rect.offsetMin;
        if (Mathf.Approximately(min.y, offset)) return;
        min.y = offset;
        _rect.offsetMin = min;
    }
}
