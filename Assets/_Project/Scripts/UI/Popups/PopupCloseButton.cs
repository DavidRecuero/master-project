using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Closes the popup this button is in. Put it on the round X button of any popup:
/// no OnClick to wire, so the same CloseButton prefab works in every popup.
/// </summary>
[RequireComponent(typeof(Button))]
public class PopupCloseButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button != null) _button.onClick.AddListener(CloseParentPopup);
    }

    private void OnDisable()
    {
        if (_button != null) _button.onClick.RemoveListener(CloseParentPopup);
    }

    public void CloseParentPopup()
    {
        PopupView popup = GetComponentInParent<PopupView>();
        if (popup != null) popup.Close();
        else Debug.LogWarning("[PopupCloseButton] Not inside a popup.");
    }
}
