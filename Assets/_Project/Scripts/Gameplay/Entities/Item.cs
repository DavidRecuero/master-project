using System.Collections;
using UnityEngine;

public class Item : MonoBehaviour
{
    public int colorID;
    public Color itemColor;
    public ItemType itemType = ItemType.Normal;
    public Vector2Int gridPosition;
    public Pipe parentPipe;

    [Header("Item Settings")]
    public float defaultScale = 0.7f;
    public int baseSortingOrder = 10;
    public Vector2 colliderSize = new Vector2(1f, 1f);

    [Header("Animation Settings")]
    public float pulseSpeed = 14f;
    public float pulseAmount = 0.02f;
    [HideInInspector] public bool inTray = false;

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer overlayRenderer; // Extra icon drawn on top of the item (e.g. "?" for hidden items)
    private Coroutine pulseCoroutine;

    /// <summary>True when this item sits on the last cell of its pipe.</summary>
    public bool IsAtExit =>
        parentPipe != null && parentPipe.HasPath && gridPosition == parentPipe.ExitPosition;

    /// <summary>
    /// Single source of truth for "can the player send this to the tray right now?".
    /// Obstacles (e.g. a barrel with lives left) will extend this rule.
    /// </summary>
    public bool CanBeCollected => !inTray && IsAtExit;

    /// <summary>The player can't see this item's color yet.</summary>
    public bool IsHidden => itemType == ItemType.Hidden;

    /// <summary>
    /// A plain item whose color is visible. Boosters that work with colors (Match, Shuffle)
    /// only touch these, so they never reveal or move obstacles.
    /// </summary>
    public bool HasKnownColor => itemType == ItemType.Normal;

    private void Awake()
    {
        EnsureSpriteRenderer();
    }

    private void OnEnable()
    {
        // Evaluates the animation on enable the game object
        CheckExitPulse();
    }

    private void OnDisable()
    {
        // Stops the courrotine if the GameObject is disabled
        StopPulse();
    }

    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null && !TryGetComponent(out spriteRenderer))
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    // Lazily creates a child SpriteRenderer used to draw an icon on top of the item
    private void EnsureOverlayRenderer()
    {
        if (overlayRenderer != null) return;

        GameObject overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(transform, false);
        overlayObj.transform.localPosition = Vector3.zero;
        overlayObj.transform.localScale = Vector3.one;

        overlayRenderer = overlayObj.AddComponent<SpriteRenderer>();
        overlayRenderer.enabled = false;
    }

    /// <summary>Convenience overload for plain colored items.</summary>
    public void Init(int id, Color color, Vector2Int pos, Pipe pipe, Sprite sprite)
    {
        Init(ItemSpawnData.Normal(id), color, pos, pipe, sprite);
    }

    public void Init(ItemSpawnData data, Color color, Vector2Int pos, Pipe pipe, Sprite sprite)
    {
        colorID = data.colorId;
        itemType = data.type;
        itemColor = color;
        gridPosition = pos;
        parentPipe = pipe;

        EnsureSpriteRenderer();

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;

        // Slightly scale down the item so it fits nicely inside the pipe
        transform.localScale = new Vector3(defaultScale, defaultScale, 1f);

        // Draw above the tilemap (Tilemap is usually layer 0)
        spriteRenderer.sortingOrder = baseSortingOrder + pipe.layer;

        // A reused pooled object might still have an icon from its previous life
        HideOverlay();

        // Collider to detect clicks or interactions
        BoxCollider2D col = gameObject.GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.size = colliderSize;
        col.enabled = true;

        CheckExitPulse();
    }

    /// <summary>
    /// Covers the real color of a Hidden item with a neutral color and an optional icon.
    /// The real color is kept in itemColor/colorID and shown again on Reveal().
    /// </summary>
    public void ApplyHiddenLook(Color hiddenColor, Sprite hiddenIcon)
    {
        if (!IsHidden) return;

        EnsureSpriteRenderer();
        spriteRenderer.color = hiddenColor;

        if (hiddenIcon != null) ShowOverlay(hiddenIcon);
    }

    /// <summary>
    /// Turns a Hidden item into a normal one showing its real color.
    /// Called by the tray right before the item is placed, so matching works as usual.
    /// </summary>
    public void Reveal()
    {
        if (!IsHidden) return;

        itemType = ItemType.Normal;

        EnsureSpriteRenderer();
        spriteRenderer.color = itemColor;
        HideOverlay();
    }

    private void ShowOverlay(Sprite icon)
    {
        EnsureOverlayRenderer();
        overlayRenderer.sprite = icon;
        overlayRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        overlayRenderer.enabled = true;
    }

    private void HideOverlay()
    {
        if (overlayRenderer != null) overlayRenderer.enabled = false;
    }

    /// <summary>
    /// Puts the item back into a neutral state before returning it to the pool,
    /// so no obstacle state leaks into the next element that reuses this GameObject.
    /// </summary>
    public void ResetForPool()
    {
        StopPulse();
        inTray = false;
        itemType = ItemType.Normal;
        parentPipe = null;
        HideOverlay();
        if (TryGetComponent(out Collider2D col)) col.enabled = true;
    }

    public void UpdateGridPosition(Vector2Int newPos)
    {
        gridPosition = newPos;
        CheckExitPulse();
    }

    /// <summary>
    /// Recolors this item in place, without touching its position or parent pipe.
    /// Used by the Shuffle booster to redistribute colors across the board.
    /// A hidden item stores the new color but keeps showing its hidden look.
    /// </summary>
    public void ApplyColor(int id, Color color)
    {
        colorID = id;
        itemColor = color;

        if (IsHidden) return;

        EnsureSpriteRenderer();
        spriteRenderer.color = color;
    }

    public void SetIdentity(int id, Color color, ItemType type)
    {
        colorID = id;
        itemColor = color;
        itemType = type;

        EnsureSpriteRenderer();
        spriteRenderer.color = color;
        HideOverlay();
    }

    public void CheckExitPulse()
    {
        if (CanBeCollected)
        {
            StartPulse();
        }
        else
        {
            StopPulse();
        }
    }

    public void StartPulse()
    {
        if (pulseCoroutine != null || !gameObject.activeInHierarchy) return;
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    public void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        transform.localScale = new Vector3(defaultScale, defaultScale, 1f);
    }

    private IEnumerator PulseRoutine()
    {
        while (!inTray)
        {
            float currentPulse = defaultScale + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = new Vector3(currentPulse, currentPulse, 1f);
            yield return null;
        }
        StopPulse();
    }
}
