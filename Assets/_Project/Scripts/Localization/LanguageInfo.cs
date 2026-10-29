/// <summary>
/// One language the game can show: its locale code ("en", "es", "ca") and the name to show
/// in the language selector, written in that same language ("English", "Español", "Català").
/// </summary>
public readonly struct LanguageInfo
{
    public string Code { get; }
    public string DisplayName { get; }

    public LanguageInfo(string code, string displayName)
    {
        Code = code;
        DisplayName = string.IsNullOrEmpty(displayName) ? code : displayName;
    }

    public override string ToString() => $"{DisplayName} ({Code})";
}
