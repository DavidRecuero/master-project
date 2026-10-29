using TMPro;
using UnityEngine;

/// <summary>
/// Credits popup, opened from Settings. The text is data, not code: one text file per language
/// (Assets/_Project/Data/Credits/Credits_en.txt, _es, _ca) in the "Assets" Asset Table, so it can be
/// edited without touching scripts and kept in sync with the Credits section of the README.
/// The music is CC-BY 3.0: its author and license have to be shown somewhere in the game, in every language.
/// The files may use TMP rich text (&lt;b&gt;...&lt;/b&gt;). Plain Latin-1 characters only: the static fonts don't hold anything else.
/// </summary>
public class CreditsPopup : PopupView
{
    [SerializeField] private TextMeshProUGUI bodyText;
    [Tooltip("Assets/credits: the credits file of each language")]
    [SerializeField] private LocalizedTextAsset creditsFile = new LocalizedTextAsset();

    private TextAsset _fileOverride;
    private bool _listening;

    /// <summary>Optional injection (tests): shows this file instead of the localized one.</summary>
    public void Initialize(TextAsset file)
    {
        StopListening();
        _fileOverride = file;
        Show(file);
    }

    protected override void OnOpened()
    {
        if (_fileOverride != null)
        {
            Show(_fileOverride);
            return;
        }

        if (creditsFile == null || creditsFile.IsEmpty)
        {
            Debug.LogWarning("[CreditsPopup] No credits file assigned.");
            Show(null);
            return;
        }

        // Called now with the file of the current language, and again if the language changes
        creditsFile.AssetChanged += OnCreditsFileLoaded;
        _listening = true;
    }

    protected override void OnClosed() => StopListening();

    private void OnDestroy() => StopListening();

    private void OnCreditsFileLoaded(TextAsset file)
    {
        if (file == null) Debug.LogWarning("[CreditsPopup] No credits file for the current language.");
        Show(file);
    }

    private void Show(TextAsset file)
    {
        if (bodyText == null) return;
        bodyText.text = file != null ? Normalize(file.text) : string.Empty;
    }

    private void StopListening()
    {
        if (!_listening) return;
        creditsFile.AssetChanged -= OnCreditsFileLoaded;
        _listening = false;
    }

    /// <summary>Windows line endings would show as extra blank lines; trailing spaces/newlines are dropped.</summary>
    public static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }
}
