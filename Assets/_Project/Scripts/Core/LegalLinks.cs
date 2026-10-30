using System.Collections.Generic;

/// <summary>
/// Public legal pages of the game. The same URL goes in Play Console (App content > Privacy policy),
/// so if it ever changes, change it in both places.
/// The page (davidrecuero.com, repo DavidRecuero-web, data/pipemessPrivacy.ts) has one section per
/// language with an anchor (#en, #es, #ca): the game opens the one of the language on screen.
/// </summary>
public static class LegalLinks
{
    public const string PrivacyPolicy = "https://davidrecuero.com/pipemess/privacy";

    /// <summary>Languages that have their own section on the privacy page. Add a code here when the page gets a new language.</summary>
    private static readonly HashSet<string> PrivacyPolicyLanguages = new HashSet<string> { "en", "es", "ca" };

    /// <summary>Privacy policy in the given language. A language without its own section opens the page at the top (English first).</summary>
    public static string PrivacyPolicyUrl(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode)) return PrivacyPolicy;

        // "es-ES" and "es" go to the same section
        string code = languageCode.Split('-', '_')[0].ToLowerInvariant();
        return PrivacyPolicyLanguages.Contains(code) ? $"{PrivacyPolicy}#{code}" : PrivacyPolicy;
    }
}
