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
    // Item sprites are drawn at exit-platform size (200 px of a 256 px cell, PPU 256).
    // Inside the tube they are shown a bit smaller, and full size once they reach the exit.
    public float defaultScale = 0.86f;
    public float exitScaleMultiplier = 1.17f;
    [Tooltip("Order inside the pipe's layer band (see BoardSortingOrder): above its tube, below the mouths")]
    public int baseSortingOrder = BoardSortingOrder.Item;
    public Vector2 colliderSize = new Vector2(1f, 1f);

    [Header("Animation Settings")]
    public float pulseSpeed = 14f;
    public float pulseAmount = 0.02f;
    [HideInInspector] public bool inTray = false;

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer overlayRenderer; // Extra icon drawn on top of the item (legacy "?" for hidden items)
    private Coroutine pulseCoroutine;

    // Finished sprite per color. When null, the item falls back to tinting its base sprite (legacy look).
    private ItemPalette palette;

    // ---- Barrel state
    private int barrelLives;
    private Sprite[] barrelDamageSprites;   // [0] = intact, [last] = most damaged
    private Color barrelFallbackColor = Color.white;
    private bool barrelContentHidden;       // The element inside comes out as Hidden

    /// <summary>True when this item sits on the last cell of its pipe.</summary>
    public bool IsAtExit =>
        parentPipe != null && parentPipe.HasPath && gridPosition == parentPipe.ExitPosition;

    /// <summary>
    /// Single source of truth for "can the player send this to the tray right now?".
    /// Obstacles (e.g. a barrel with lives left) will extend this rule.
    /// </summary>
    public bool CanBeCollected => !inTray && IsAtExit && !IsBarrel;

    /// <summary>The player can't see this item's color yet.</summary>
    public bool IsHidden => itemType == ItemType.Hidden;

    /// <summary>Can't be tapped until it breaks and releases the element inside.</summary>
    public bool IsBarrel => itemType == ItemType.Barrel;

    /// <summary>Lives left (only for barrels).</summary>
    public int BarrelLives => barrelLives;

    /// <summary> A plain item whose color is visible. Boosters that work with colors (Match, Shuffle) only touch these, so they never reveal or move obstacles. </summary>
    public bool HasKnownColor => itemType == ItemType.Normal;

    /// <summary>Resting scale: full size on the exit platform, smaller inside the tube and in the tray.</summary>
    public float BaseScale => (!inTray && IsAtExit) ? defaultScale * exitScaleMultiplier : defaultScale;

    /// <summary>The palette this item uses to pick its sprite. Set it before Init.</summary>
    public ItemPalette Palette => palette;

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

    /// <summary>
    /// Assigns the palette used to draw this item. With a palette every color has its own finished sprite;
    /// without one, the base sprite passed to Init is tinted with the item color.
    /// </summary>
    public void SetPalette(ItemPalette itemPalette)
    {
        palette = itemPalette;
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
        barrelContentHidden = data.contentHidden;
        barrelLives = 0;
        barrelDamageSprites = null;
        itemColor = color;
        gridPosition = pos;
        parentPipe = pipe;

        EnsureSpriteRenderer();

        spriteRenderer.sprite = sprite;
        ShowColorLook();

        // Smaller inside the tube, full size on the exit platform
        SetScale(BaseScale);

        // Above its own tube, below every pipe of a higher layer
        spriteRenderer.sortingOrder = BoardSortingOrder.For(pipe.layer, baseSortingOrder);

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
    /// Covers the real color of a Hidden item. Uses the palette's hidden sprite (grey drop with "?") when there is one;
    /// otherwise tints the base sprite with hiddenColor and draws hiddenIcon on top (legacy look).
    /// The real color is kept in itemColor/colorID and shown again on Reveal().
    /// </summary>
    public void ApplyHiddenLook(Color hiddenColor, Sprite hiddenIcon)
    {
        if (!IsHidden) return;

        EnsureSpriteRenderer();

        Sprite hiddenSprite = palette != null ? palette.HiddenSprite : null;
        if (hiddenSprite != null)
        {
            spriteRenderer.sprite = hiddenSprite;
            spriteRenderer.color = Color.white;
            HideOverlay();
            return;
        }

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

        ShowColorLook();
        HideOverlay();
    }

    // ---------- Barrel ----------

    /// <summary>
    /// Sets up the barrel look and lives. damageSprites[0] is the intact barrel and each next sprite is more damaged. If no sprites are given, the item sprite is tinted with fallbackColor.
    /// </summary>
    public void ApplyBarrelLook(int lives, Sprite[] damageSprites, Color fallbackColor)
    {
        if (!IsBarrel) return;

        barrelLives = Mathf.Max(1, lives);
        barrelDamageSprites = damageSprites;
        barrelFallbackColor = fallbackColor;

        HideOverlay();
        UpdateBarrelSprite();
        CheckExitPulse();
    }

    /// <summary>Removes lives from the barrel. Returns true when it has no lives left.</summary>
    public bool TakeBarrelDamage(int amount = 1)
    {
        if (!IsBarrel) return false;

        barrelLives = Mathf.Max(0, barrelLives - amount);
        UpdateBarrelSprite();
        return barrelLives == 0;
    }

    /// <summary>
    /// Turns the barrel into the element it was holding (normal or hidden), keeping its color id, position and pipe. Call ApplyHiddenLook afterwards to cover a hidden content.
    /// </summary>
    public void BreakOpen(Sprite itemSprite)
    {
        if (!IsBarrel) return;

        itemType = barrelContentHidden ? ItemType.Hidden : ItemType.Normal;
        barrelLives = 0;
        barrelDamageSprites = null;

        EnsureSpriteRenderer();
        spriteRenderer.sprite = itemSprite;
        ShowColorLook();

        CheckExitPulse();
    }

    // Shows the sprite that matches the damage taken so far
    private void UpdateBarrelSprite()
    {
        EnsureSpriteRenderer();

        if (barrelDamageSprites == null || barrelDamageSprites.Length == 0)
        {
            spriteRenderer.color = barrelFallbackColor;
            return;
        }

        int index = Mathf.Clamp(barrelDamageSprites.Length - barrelLives, 0, barrelDamageSprites.Length - 1);

        spriteRenderer.sprite = barrelDamageSprites[index];
        spriteRenderer.color = Color.white; // Show the barrel art untinted
    }

    // Shows the item's real color: the palette sprite for its colorID, or the base sprite tinted with itemColor
    private void ShowColorLook()
    {
        EnsureSpriteRenderer();

        Sprite colorSprite = palette != null ? palette.GetSprite(colorID) : null;
        if (colorSprite != null)
        {
            spriteRenderer.sprite = colorSprite;
            spriteRenderer.color = Color.white; // The sprite already carries its color
        }
        else
        {
            spriteRenderer.color = itemColor;
        }
    }

    private void SetScale(float scale)
    {
        transform.localScale = new Vector3(scale, scale, 1f);
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
        barrelLives = 0;
        barrelDamageSprites = null;
        barrelContentHidden = false;
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

        ShowColorLook();
    }

    /// <summary>
    /// Changes color and type at once and shows the item as visible.
    /// Call ApplyHiddenLook afterwards if the new type is Hidden.
    /// </summary>
    public void SetIdentity(int id, Color color, ItemType type)
    {
        colorID = id;
        itemColor = color;
        itemType = type;

        ShowColorLook();
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
        SetScale(BaseScale);
    }

    private IEnumerator PulseRoutine()
    {
        while (!inTray)
        {
            SetScale(BaseScale + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
            yield return null;
        }
        StopPulse();
    }
}
