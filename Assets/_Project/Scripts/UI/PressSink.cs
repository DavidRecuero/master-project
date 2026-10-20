using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tactile press for chunky buttons: while the finger is down, the button face (icon, badge...)
/// sinks onto its drop shadow. Pair it with the Button's Sprite Swap to the "Pressed" sprite.
/// </summary>
public class PressSink : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("What moves down: the child holding the icon and the badge")]
    [SerializeField] private RectTransform target;
    [Tooltip("Canvas units. 15 = the 5 pt shadow of the booster button at @3x")]
    [SerializeField] private float sinkDistance = 15f;

    private Selectable _selectable;
    private Vector2 _restPosition;
    private bool _pressed;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        if (target != null) _restPosition = target.anchoredPosition;
    }

    private void OnDisable()
    {
        Release();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (target == null || _pressed) return;
        if (_selectable != null && !_selectable.IsInteractable()) return;

        _pressed = true;
        target.anchoredPosition = _restPosition + Vector2.down * sinkDistance;
    }

    public void OnPointerUp(PointerEventData eventData) => Release();
    public void OnPointerExit(PointerEventData eventData) => Release();

    private void Release()
    {
        if (!_pressed || target == null) return;
        _pressed = false;
        target.anchoredPosition = _restPosition;
    }
}
