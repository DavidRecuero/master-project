using System;
using System.Collections.Generic;

/// <summary>
/// Language of the game: startup, the list of languages and the player's choice.
///
/// Texts are NOT read through this interface: every text uses the Localization package directly
/// (LocalizeStringEvent on the TMP text, or a LocalizedString field), which also refreshes it when
/// the language changes. This interface only covers what the game code decides (Boot waits for
/// the tables, Settings changes the language), so that code can be tested with a fake.
///
/// Two modes:
///  - Automatic (default): the game follows the device language. Nothing is saved.
///  - Manual: the player chose a language in Settings. It is saved and wins over the device language
///    until the player picks "Device language" again.
/// </summary>
public interface ILocalizationService
{
    /// <summary>True once the locales and the preloaded tables are loaded.</summary>
    bool IsReady { get; }

    /// <summary>Starts loading (if it has not started yet) and calls onReady when it is done. Called right away if it is already done.</summary>
    void Initialize(Action onReady);

    /// <summary>Languages the game ships with, in the order set in the Localization Settings. Empty until ready.</summary>
    IReadOnlyList<LanguageInfo> Languages { get; }

    /// <summary>Code of the language on screen ("en", "es", "ca"), or null before it is ready.</summary>
    string CurrentLanguage { get; }

    /// <summary>True while no language has been chosen by hand (the device language decides).</summary>
    bool FollowsDevice { get; }

    /// <summary>Manual mode: shows that language now and remembers it for the next sessions.</summary>
    void SetLanguage(string code);

    /// <summary>Automatic mode: forgets the manual choice and goes back to the device language.</summary>
    void UseDeviceLanguage();

    /// <summary>Raised after the language on screen changes.</summary>
    event Action LanguageChanged;
}
