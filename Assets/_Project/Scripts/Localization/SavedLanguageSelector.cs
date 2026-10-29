using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Startup locale selector that starts the game in the language the player chose in Settings.
/// Put it FIRST in Localization Settings > Locale Selectors, then System Locale Selector, then
/// Specific Locale Selector (English) as the last fallback.
///
/// Why not the built-in Player Pref Locale Selector: it also saves the locale picked at startup,
/// so after the first launch the device language is pinned for good and "automatic" stops working
/// (changing the phone language would no longer change the game). This one only reads what
/// UnityLocalizationService.SetLanguage saved, so automatic stays automatic.
///
/// Returning null hands the decision to the next selector (no manual choice, or a language that
/// is no longer in the game).
/// </summary>
[Serializable]
public class SavedLanguageSelector : IStartupLocaleSelector
{
    public Locale GetStartupLocale(ILocalesProvider availableLocales)
    {
        var store = new LanguageSettingsStore(new PlayerPrefsStorageProvider());
        return Resolve(store.Language, availableLocales);
    }

    /// <summary>The saved locale if the game still has it; null otherwise.</summary>
    public static Locale Resolve(string savedCode, ILocalesProvider availableLocales)
    {
        if (string.IsNullOrEmpty(savedCode) || availableLocales == null) return null;
        return availableLocales.GetLocale(new LocaleIdentifier(savedCode));
    }
}
