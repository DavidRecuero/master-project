using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// "Hold to confirm" for irreversible actions: the action only fires after the button is held down
/// for holdSeconds. Releasing, or dragging the finger off the button, cancels and empties the fill.
///
/// The fill is created at runtime: a darker copy of the button's own sprite (the "pressed" one, since
/// the button is pressed while filling), Sliced like the button so its corners and border keep their
/// shape, inside a RectMask2D that grows from left to right. (An Image of type Filled can't be used:
/// it ignores the 9-slice borders and stretches the whole sprite.)
/// Put it next to the Button. Listen to Completed instead of Button.onClick.
/// </summary>
public class HoldToConfirm : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Seconds the button has to be held down")]
    [Min(0.1f)]
    [SerializeField] private float holdSeconds = 2f;
    [Tooltip("How fast the fill empties after letting go, in fills per second")]
    [SerializeField] private float drainSpeed = 4f;

    [Header("Fill")]
    [Tooltip("Multiplies the button sprite: darker than the button, slightly see-through")]
    [SerializeField] private Color fillColor = new Color(0.45f, 0.08f, 0.08f, 0.55f);

    /// <summary>Raised once each time the hold reaches the end.</summary>
    public event Action Completed;

    public bool IsHolding { get; private set; }

    /// <summary>0 = not held, 1 = done.</summary>
    public float Progress { get; private set; }

    public float HoldSeconds
    {
        get => holdSeconds;
        set => holdSeconds = Mathf.Max(0.1f, value);
    }

    private Selectable _selectable;
    private RectTransform _rect;
    private RectTransform _mask;   // grows with the progress
    private RectTransform _fill;   // always as wide as the button, clipped by the mask

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        _rect = transform as RectTransform;
        CreateFill();
        ShowProgress();
    }

    private void OnDisable()
    {
        // Hidden or closed mid-hold: start from zero next time
        IsHolding = false;
        Progress = 0f;
        ShowProgress();
    }

    private void Update() => Tick(Time.unscaledDeltaTime);

    // --- Input (public so the tests can drive it without an EventSystem) ---

    public void OnPointerDown(PointerEventData eventData) => Press();
    public void OnPointerUp(PointerEventData eventData) => Release();
    public void OnPointerExit(PointerEventData eventData) => Release();

    public void Press()
    {
        if (_selectable != null && !_selectable.IsInteractable()) return;
        IsHolding = true;
    }

    public void Release() => IsHolding = false;

    /// <summary>Advances the hold by deltaTime seconds (Update calls it every frame).</summary>
    public void Tick(float deltaTime)
    {
        if (IsHolding)
        {
            Progress = Mathf.Min(1f, Progress + deltaTime / holdSeconds);

            if (Progress >= 1f)
            {
                // A new hold is needed to fire again
                IsHolding = false;
                Progress = 0f;
                ShowProgress();
                Completed?.Invoke();
                return;
            }
        }
        else if (Progress > 0f)
        {
            Progress = Mathf.Max(0f, Progress - deltaTime * drainSpeed);
        }

        ShowProgress();
    }

    private void ShowProgress()
    {
        if (_mask == null || _rect == null) return;

        float width = _rect.rect.width;
        _fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _mask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * Progress);
        _mask.gameObject.SetActive(Progress > 0f);
    }

    /// <summary>
    /// HoldFill (RectMask2D, left-anchored, width = progress) > Fill (Sliced Image, full button size).
    /// First sibling: over the button background, under the label.
    /// </summary>
    private void CreateFill()
    {
        if (_rect == null) return;

        var maskGo = new GameObject("HoldFill", typeof(RectTransform), typeof(RectMask2D), typeof(LayoutElement));
        maskGo.layer = gameObject.layer;
        maskGo.GetComponent<LayoutElement>().ignoreLayout = true;
        _mask = (RectTransform)maskGo.transform;
        _mask.SetParent(transform, false);
        _mask.SetAsFirstSibling();
        AnchorLeftFullHeight(_mask);

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.layer = gameObject.layer;
        _fill = (RectTransform)fillGo.transform;
        _fill.SetParent(_mask, false);
        AnchorLeftFullHeight(_fill);

        var image = fillGo.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = fillColor;

        var background = GetComponent<Image>();
        if (background != null)
        {
            image.sprite = PressedSprite() ?? background.sprite;
            image.type = background.type == Image.Type.Filled ? Image.Type.Simple : background.type;
            image.pixelsPerUnitMultiplier = background.pixelsPerUnitMultiplier;
            image.fillCenter = true;
        }
    }

    /// <summary>The button shows its "pressed" sprite while held; the fill matches it.</summary>
    private Sprite PressedSprite()
    {
        if (_selectable == null || _selectable.transition != Selectable.Transition.SpriteSwap) return null;
        return _selectable.spriteState.pressedSprite;
    }

    private static void AnchorLeftFullHeight(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 0f);
    }
}
