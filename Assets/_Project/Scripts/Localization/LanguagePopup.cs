using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Popup with every language of the game, opened from the language button in Settings.
/// One LanguageOptionView per language, built when it opens, with a mark on the language on screen.
/// Tapping another language sets it (it wins over the device language from then on) and closes the popup.
/// Tapping the one already on screen just closes it (the game keeps following the device).
/// Adding a language to the Localization Settings adds it here with no code change.
/// </summary>
public class LanguagePopup : PopupView
{
    [Tooltip("Parent of the rows (with a VerticalLayoutGroup)")]
    [SerializeField] private RectTransform optionsContainer;
    [Tooltip("LanguageOption prefab: one row per language")]
    [SerializeField] private LanguageOptionView optionPrefab;

    private ILocalizationService _localization;
    private readonly List<LanguageOptionView> _options = new List<LanguageOptionView>();

    public IReadOnlyList<LanguageOptionView> Options => _options;

    /// <summary>Optional injection (tests). Without it, the shared UnityLocalizationService is used.</summary>
    public void Initialize(ILocalizationService localization)
    {
        _localization = localization;
        Build();
    }

    protected override void OnOpened()
    {
        _localization ??= UnityLocalizationService.Instance;
        Build();
    }

    /// <summary>Sets the language (if it is another one) and closes the popup.</summary>
    public void Select(string code)
    {
        if (_localization != null && !string.IsNullOrEmpty(code) && code != _localization.CurrentLanguage)
            _localization.SetLanguage(code);

        Close();
    }

    private void Build()
    {
        foreach (LanguageOptionView option in _options)
        {
            if (option != null) PopupService.DestroyObject(option.gameObject);
        }
        _options.Clear();

        if (_localization == null || optionsContainer == null || optionPrefab == null)
        {
            if (optionsContainer == null || optionPrefab == null)
                Debug.LogWarning("[LanguagePopup] Options container or option prefab not assigned.");
            return;
        }

        string current = _localization.CurrentLanguage;
        foreach (LanguageInfo language in _localization.Languages)
        {
            LanguageOptionView option = Instantiate(optionPrefab, optionsContainer, false);
            option.name = $"Option_{language.Code}";
            option.Setup(language, language.Code == current, Select);
            _options.Add(option);
        }
    }
}
