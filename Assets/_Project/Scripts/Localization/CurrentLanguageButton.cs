using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Language button of the Settings popup. Shows the language on screen, written in that language
/// (the device one while the player hasn't picked any), and opens the LanguagePopup on top when tapped.
/// Follows language changes, so it is always up to date.
/// </summary>
public class CurrentLanguageButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [Tooltip("LanguagePopup prefab, opened on top of Settings")]
    [SerializeField] private LanguagePopup languagePopupPrefab;

    private ILocalizationService _localization;
    private IPopupService _popups;
    private bool _subscribed;

    /// <summary>Optional injection (tests). Without it: the shared UnityLocalizationService, and the service that opened the parent popup.</summary>
    public void Initialize(ILocalizationService localization, IPopupService popups = null)
    {
        Unsubscribe();
        _localization = localization;
        if (popups != null) _popups = popups;
        Subscribe();
        Refresh();
    }

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(OnClicked);
    }

    private void OnEnable()
    {
        _localization ??= UnityLocalizationService.Instance;
        Subscribe();
        Refresh();
    }

    private void OnDisable() => Unsubscribe();

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClicked);
    }

    public void OnClicked()
    {
        if (languagePopupPrefab == null)
        {
            Debug.LogWarning("[CurrentLanguageButton] No LanguagePopup prefab assigned.");
            return;
        }

        // The service that opened Settings, so the language popup stacks on top of it (and closes with it)
        IPopupService popups = _popups ?? GetComponentInParent<PopupView>()?.Service ?? PopupService.Instance;
        if (popups == null)
        {
            Debug.LogWarning("[CurrentLanguageButton] No PopupService found. Start from Boot scene.");
            return;
        }

        popups.Open(languagePopupPrefab);
    }

    private void Refresh()
    {
        if (label == null || _localization == null) return;
        label.text = CurrentName(_localization.CurrentLanguage, _localization.Languages);
    }

    /// <summary>Name of the language on screen, in its own language. The code if it is not in the list.</summary>
    public static string CurrentName(string currentCode, IReadOnlyList<LanguageInfo> languages)
    {
        if (languages != null)
        {
            foreach (LanguageInfo language in languages)
            {
                if (language.Code == currentCode) return language.DisplayName;
            }
        }

        return currentCode ?? string.Empty;
    }

    private void OnLanguageChanged() => Refresh();

    private void Subscribe()
    {
        if (_subscribed || _localization == null) return;
        _localization.LanguageChanged += OnLanguageChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || _localization == null) return;
        _localization.LanguageChanged -= OnLanguageChanged;
        _subscribed = false;
    }
}
