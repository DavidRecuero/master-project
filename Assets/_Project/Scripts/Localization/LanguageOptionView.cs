using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row of the language popup: a button with the language name, written in that language
/// ("English", "Español", "Català"), and a mark on the language on screen.
/// A dumb view: the LanguagePopup tells it what to show and what to do when it is tapped.
/// </summary>
public class LanguageOptionView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [Tooltip("Shown only on the language on screen")]
    [SerializeField] private GameObject selectedMark;

    private Action<string> _onSelected;

    public string Code { get; private set; }
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(Click);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Click);
    }

    public void Setup(LanguageInfo language, bool selected, Action<string> onSelected)
    {
        Code = language.Code;
        IsSelected = selected;
        _onSelected = onSelected;

        // A language name is never translated: each one is shown in its own language
        if (label != null) label.text = language.DisplayName;
        if (selectedMark != null) selectedMark.SetActive(selected);
    }

    /// <summary>Same as tapping the row.</summary>
    public void Click() => _onSelected?.Invoke(Code);
}
