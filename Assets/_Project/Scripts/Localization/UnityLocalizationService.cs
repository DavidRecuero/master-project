using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// ILocalizationService on top of Unity's Localization package (LocalizationSettings).
/// A thin wrapper: the package does the real work (locales, tables, startup selectors);
/// this class adds the "automatic vs manual" language rule and keeps the game code testable.
///
/// One shared instance (Instance), because it mirrors a static Unity system and listens to its
/// static SelectedLocaleChanged event once.
/// </summary>
public class UnityLocalizationService : ILocalizationService
{
    private static UnityLocalizationService _instance;

    /// <summary>The shared service, saving the manual choice in PlayerPrefs.</summary>
    public static UnityLocalizationService Instance =>
        _instance ??= new UnityLocalizationService(new LanguageSettingsStore(new PlayerPrefsStorageProvider()));

    // With "Enter Play Mode Options" (no domain reload) statics survive between Play sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private readonly LanguageSettingsStore _store;
    private readonly List<LanguageInfo> _languages = new List<LanguageInfo>();

    public event Action LanguageChanged;

    public UnityLocalizationService(LanguageSettingsStore store)
    {
        _store = store;
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    public bool IsReady => LocalizationSettings.InitializationOperation.IsDone;

    public void Initialize(Action onReady)
    {
        AsyncOperationHandle<LocalizationSettings> operation = LocalizationSettings.InitializationOperation;

        if (operation.IsDone)
        {
            LogResult(operation);
            onReady?.Invoke();
            return;
        }

        operation.Completed += handle =>
        {
            LogResult(handle);
            onReady?.Invoke();
        };
    }

    public IReadOnlyList<LanguageInfo> Languages
    {
        get
        {
            _languages.Clear();
            if (!IsReady) return _languages;

            foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
            {
                if (locale != null) _languages.Add(new LanguageInfo(locale.Identifier.Code, locale.LocaleName));
            }

            return _languages;
        }
    }

    public string CurrentLanguage => IsReady ? LocalizationSettings.SelectedLocale?.Identifier.Code : null;

    public bool FollowsDevice => !_store.HasManualLanguage;

    public void SetLanguage(string code)
    {
        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
        if (locale == null)
        {
            Debug.LogWarning($"[Localization] Language '{code}' is not in the game.");
            return;
        }

        _store.Language = code;
        LocalizationSettings.SelectedLocale = locale;
    }

    public void UseDeviceLanguage()
    {
        _store.Clear();
        Locale deviceLocale = ResolveAutomaticLocale();
        if (deviceLocale != null) LocalizationSettings.SelectedLocale = deviceLocale;
    }

    /// <summary>
    /// Runs the startup selectors configured in the Localization Settings, skipping the saved choice,
    /// so "automatic" means exactly what the game does on a first launch (device language, then English).
    /// </summary>
    private static Locale ResolveAutomaticLocale()
    {
        ILocalesProvider locales = LocalizationSettings.AvailableLocales;

        foreach (IStartupLocaleSelector selector in LocalizationSettings.StartupLocaleSelectors)
        {
            if (selector == null || selector is SavedLanguageSelector) continue;

            Locale locale = selector.GetStartupLocale(locales);
            if (locale != null) return locale;
        }

        return LocalizationSettings.ProjectLocale;
    }

    private void OnSelectedLocaleChanged(Locale locale) => LanguageChanged?.Invoke();

    private static void LogResult(AsyncOperationHandle<LocalizationSettings> operation)
    {
        if (operation.Status == AsyncOperationStatus.Succeeded)
            Debug.Log($"[Localization] Ready. Language: {LocalizationSettings.SelectedLocale?.Identifier.Code}");
        else
            Debug.LogError($"[Localization] Initialization failed: {operation.OperationException}");
    }
}
