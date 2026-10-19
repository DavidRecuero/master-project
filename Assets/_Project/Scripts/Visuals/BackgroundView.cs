using UnityEngine;

/// <summary>
/// Paints the scene background: a seamless tile (Background_Tile) that always fills the camera view,
/// whatever the level size or screen ratio. Also sets the camera clear colour to the same base tone,
/// so nothing dark can flash at the edges.
/// Put it on an empty GameObject at the root of the scene.
/// </summary>
[ExecuteAlways]
public class BackgroundView : MonoBehaviour
{
    public Sprite tileSprite;
    public Color baseColor = new Color32(0xE3, 0xEE, 0xF6, 0xFF);
    [Tooltip("Behind everything, the board panel is -50")]
    public int sortingOrder = -100;
    [Tooltip("Extra world units drawn around the view")]
    public float overscan = 1f;
    public Camera targetCamera;

    private SpriteRenderer _renderer;

    /// <summary>Size the tiled sprite needs to cover an orthographic view, plus overscan on every side.</summary>
    public static Vector2 CoverSize(float orthographicSize, float aspect, float overscan)
    {
        float height = orthographicSize * 2f + overscan * 2f;
        float width = orthographicSize * 2f * aspect + overscan * 2f;
        return new Vector2(width, height);
    }

    private void OnEnable()
    {
        EnsureRenderer();
        Fit();
    }

    private void LateUpdate()
    {
        Fit();
    }

    private void EnsureRenderer()
    {
        if (_renderer != null) return;

        Transform found = transform.Find("BackgroundTiles");
        if (found == null)
        {
            var go = new GameObject("BackgroundTiles");
            go.transform.SetParent(transform, false);
            found = go.transform;
        }

        _renderer = found.GetComponent<SpriteRenderer>();
        if (_renderer == null) _renderer = found.gameObject.AddComponent<SpriteRenderer>();
    }

    private void Fit()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = baseColor;

        EnsureRenderer();
        _renderer.sprite = tileSprite;
        _renderer.enabled = tileSprite != null;
        _renderer.drawMode = SpriteDrawMode.Tiled;
        _renderer.tileMode = SpriteTileMode.Continuous;
        _renderer.sortingOrder = sortingOrder;
        _renderer.size = CoverSize(cam.orthographicSize, cam.aspect, overscan);

        // Follow the camera, snapped to the tile so the pattern stays put between levels
        Vector3 c = cam.transform.position;
        float step = tileSprite != null ? tileSprite.bounds.size.x : 1f;
        float x = Mathf.Round(c.x / step) * step;
        float y = Mathf.Round(c.y / step) * step;
        _renderer.transform.position = new Vector3(x, y, 0f);
        _renderer.size += new Vector2(step, step) * 2f;   // Covers the snapping offset
    }
}
