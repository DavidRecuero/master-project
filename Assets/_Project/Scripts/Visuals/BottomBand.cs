using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Blue strip at the bottom of the screen where the boosters live (design: 101 pt + the bottom safe area,
/// with an 8 pt darker edge on top). It sizes itself in real screen pixels, so it works with any Canvas Scaler,
/// and tells the camera and the tray how much of the screen it covers.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[ExecuteAlways]
public class BottomBand : MonoBehaviour
{
    [Header("Design (iPhone 13 mockup, points)")]
    [Tooltip("Width of the mockup the sizes come from")]
    public float designWidthPt = 390f;
    [Tooltip("Height of the mockup, used to cap the size on wide screens (tablets)")]
    public float designHeightPt = 844f;
    [Tooltip("Band height above the safe area (135 pt in the mockup minus the 34 pt home indicator)")]
    public float bandHeightPt = 101f;
    [Tooltip("Darker edge on top of the band")]
    public float edgeHeightPt = 8f;

    [Header("Colors")]
    public Color bandColor = new Color32(0x3B, 0x5A, 0x8C, 0xFF);
    public Color edgeColor = new Color32(0x2C, 0x45, 0x70, 0xFF);

    private RectTransform _rect;
    private RectTransform _edge;
    private Canvas _canvas;

    /// <summary>Points to pixels: follows the width, but never grows past the height ratio (wide screens).</summary>
    public static float PixelsPerPoint(float screenWidth, float screenHeight, float designWidth, float designHeight)
    {
        return Mathf.Min(screenWidth / designWidth, screenHeight / designHeight);
    }

    /// <summary>Total band height in screen pixels, safe area included.</summary>
    public static float HeightPixels(float screenWidth, float screenHeight, float safeBottomPixels,
                                     float bandPt, float designWidth, float designHeight)
    {
        return bandPt * PixelsPerPoint(screenWidth, screenHeight, designWidth, designHeight) + safeBottomPixels;
    }

    /// <summary>Band height in screen pixels for the current screen.</summary>
    public float HeightPixelsNow =>
        HeightPixels(Screen.width, Screen.height, Screen.safeArea.yMin, bandHeightPt, designWidthPt, designHeightPt);

    /// <summary>Part of the screen height covered by the band (0..1).</summary>
    public float ScreenFraction => Screen.height > 0 ? Mathf.Clamp01(HeightPixelsNow / Screen.height) : 0f;

    /// <summary>World Y of the band's top edge, as seen by an orthographic camera.</summary>
    public float TopWorldY(Camera cam)
    {
        if (cam == null) return 0f;
        float bottom = cam.transform.position.y - cam.orthographicSize;
        return bottom + ScreenFraction * cam.orthographicSize * 2f;
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

    private void Apply()
    {
        _rect ??= GetComponent<RectTransform>();
        _canvas ??= GetComponentInParent<Canvas>();
        float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.rootCanvas.scaleFactor : 1f;

        // Full width, glued to the bottom
        _rect.anchorMin = new Vector2(0f, 0f);
        _rect.anchorMax = new Vector2(1f, 0f);
        _rect.pivot = new Vector2(0.5f, 0f);
        _rect.anchoredPosition = Vector2.zero;
        _rect.sizeDelta = new Vector2(0f, HeightPixelsNow / scale);

        // The band's own fill. Added here too, in case the component went on an object without an Image.
        Image image = GetComponent<Image>();
        if (image == null) image = gameObject.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = bandColor;
        image.raycastTarget = true;   // Taps on the band never reach the board

        EnsureEdge();
        float ppp = PixelsPerPoint(Screen.width, Screen.height, designWidthPt, designHeightPt);
        _edge.sizeDelta = new Vector2(0f, edgeHeightPt * ppp / scale);
    }

    private void EnsureEdge()
    {
        if (_edge != null) return;

        Transform found = transform.Find("Edge");
        if (found == null)
        {
            var go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            found = go.transform;
        }

        _edge = (RectTransform)found;
        _edge.anchorMin = new Vector2(0f, 1f);
        _edge.anchorMax = new Vector2(1f, 1f);
        _edge.pivot = new Vector2(0.5f, 1f);
        _edge.anchoredPosition = Vector2.zero;
        _edge.SetAsFirstSibling();

        Image edgeImage = _edge.GetComponent<Image>();
        edgeImage.color = edgeColor;
        edgeImage.raycastTarget = false;
    }
}