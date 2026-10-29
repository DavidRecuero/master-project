using System;
using System.Collections.Generic;

/// <summary>In-memory language service: records what the code under test asked for.</summary>
public class FakeLocalizationService : ILocalizationService
{
    public readonly List<LanguageInfo> LanguageList = new List<LanguageInfo>
    {
        new LanguageInfo("en", "English"),
        new LanguageInfo("es", "Español"),
        new LanguageInfo("ca", "Català"),
    };

    public bool IsReady { get; set; } = true;
    public string CurrentLanguage { get; set; } = "en";
    public bool FollowsDevice { get; set; } = true;
    public string DeviceLanguage { get; set; } = "en";

    public int UseDeviceLanguageCalls { get; private set; }
    public readonly List<string> SetLanguageCalls = new List<string>();

    public event Action LanguageChanged;

    public IReadOnlyList<LanguageInfo> Languages => LanguageList;

    public void Initialize(Action onReady) => onReady?.Invoke();

    public void SetLanguage(string code)
    {
        SetLanguageCalls.Add(code);
        FollowsDevice = false;
        CurrentLanguage = code;
        LanguageChanged?.Invoke();
    }

    public void UseDeviceLanguage()
    {
        UseDeviceLanguageCalls++;
        FollowsDevice = true;
        CurrentLanguage = DeviceLanguage;
        LanguageChanged?.Invoke();
    }

    public int Subscribers => LanguageChanged?.GetInvocationList().Length ?? 0;
}
