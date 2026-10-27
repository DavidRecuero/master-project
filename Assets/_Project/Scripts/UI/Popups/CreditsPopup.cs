using TMPro;
using UnityEngine;

/// <summary>
/// Credits popup, opened from Settings. The text is data, not code: it lives in a text file
/// (Assets/_Project/Data/Credits.txt) assigned in the prefab, so it can be edited without
/// touching scripts and kept in sync with the Credits section of the README.
/// The music is CC-BY 3.0: its author and license have to be shown somewhere in the game.
/// The file may use TMP rich text (&lt;b&gt;...&lt;/b&gt;). Plain Latin-1 characters only: the static fonts don't hold anything else.
/// </summary>
public class CreditsPopup : PopupView
{
    [SerializeField] private TextMeshProUGUI bodyText;
    [Tooltip("Text file with the credits (Data/Credits.txt)")]
    [SerializeField] private TextAsset creditsFile;

    /// <summary>Optional injection (tests).</summary>
    public void Initialize(TextAsset file)
    {
        creditsFile = file;
        Refresh();
    }

    protected override void OnOpened() => Refresh();

    private void Refresh()
    {
        if (bodyText == null) return;

        if (creditsFile == null)
        {
            Debug.LogWarning("[CreditsPopup] No credits file assigned.");
            bodyText.text = string.Empty;
            return;
        }

        bodyText.text = Normalize(creditsFile.text);
    }

    /// <summary>Windows line endings would show as extra blank lines; trailing spaces/newlines are dropped.</summary>
    public static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }
}
