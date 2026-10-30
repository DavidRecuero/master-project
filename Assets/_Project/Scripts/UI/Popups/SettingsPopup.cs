using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// Settings popup: music and SFX on/off, the language selector, the button that opens the credits
/// on top, the button that opens the privacy policy in the browser (Google Play requires it to be
/// reachable from inside the game), and a footer with the game version and the player ID (for
/// support and bug reports).
/// It only talks to IAudioService, which already saves the audio choices on the device.
/// The language selector is its own component (LanguageDropdown) inside the popup.
/// Every fixed label of the prefab uses a LocalizeStringEvent; only the footer is filled from here.
/// </summary>
public class SettingsPopup : PopupView
{
    public const string VersionVariable = "version";
    public const string IdVariable = "id";

    [Header("Audio")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;

    [Header("Credits")]
    [SerializeField] private Button creditsButton;
    [Tooltip("CreditsPopup prefab, opened on top of this one")]
    [SerializeField] private CreditsPopup creditsPopupPrefab;

    [Header("Privacy")]
    [Tooltip("Opens LegalLinks.PrivacyPolicy in the browser, in the language on screen")]
    [SerializeField] private Button privacyButton;

    [Header("Info")]
    [Tooltip("LocalizeStringEvent of the small text at the bottom. Leave its String Reference empty: the popup sets it.")]
    [SerializeField] private LocalizeStringEvent infoLabel;
    [Tooltip("UI/settings.info, Smart String with {version} and {id}")]
    [SerializeField] private LocalizedString infoText = new LocalizedString();

    private IAudioService _audio;
    private IUserDataProvider _userData;
    private string _version;
    private ILocalizationService _localization;
    private IUrlOpener _urlOpener;

    /// <summary>
    /// Optional injection (tests). Without it, the AudioManager, the UserDataManager, Application.version,
    /// the shared UnityLocalizationService and the device browser are used.
    /// </summary>
    public void Initialize(IAudioService audio, IUserDataProvider userData = null, string version = null,
        ILocalizationService localization = null, IUrlOpener urlOpener = null)
    {
        _audio = audio;
        if (userData != null) _userData = userData;
        if (version != null) _version = version;
        if (localization != null) _localization = localization;
        if (urlOpener != null) _urlOpener = urlOpener;
        Refresh();
    }

    private void Awake()
    {
        if (musicToggle != null) musicToggle.onValueChanged.AddListener(OnMusicToggled);
        if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(OnSfxToggled);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsClicked);
        if (privacyButton != null) privacyButton.onClick.AddListener(OnPrivacyClicked);
    }

    private void OnDestroy()
    {
        if (musicToggle != null) musicToggle.onValueChanged.RemoveListener(OnMusicToggled);
        if (sfxToggle != null) sfxToggle.onValueChanged.RemoveListener(OnSfxToggled);
        if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
        if (privacyButton != null) privacyButton.onClick.RemoveListener(OnPrivacyClicked);
    }

    protected override void OnOpened()
    {
        _audio ??= AudioManager.Instance;
        _userData ??= UserDataManager.Instance;
        _localization ??= UnityLocalizationService.Instance;
        if (_audio == null) Debug.LogWarning("[SettingsPopup] No AudioManager found. Start from Boot scene.");

        Refresh();
    }

    /// <summary>Puts the toggles on the saved state without firing their events (that would save it again), and fills the footer.</summary>
    private void Refresh()
    {
        if (_audio != null)
        {
            if (musicToggle != null) musicToggle.SetIsOnWithoutNotify(_audio.MusicEnabled);
            if (sfxToggle != null) sfxToggle.SetIsOnWithoutNotify(_audio.SfxEnabled);
        }

        // The full ID is shown (not cut) so the player can send it with a bug report.
        // Variables first, then the string: it is never formatted without them.
        infoText.SetVariable(VersionVariable, SafeValue(_version ?? Application.version));
        infoText.SetVariable(IdVariable, SafeValue(_userData?.UserId));

        if (infoLabel != null && !ReferenceEquals(infoLabel.StringReference, infoText))
            infoLabel.StringReference = infoText;
    }

    /// <summary>A missing value shows as a dash, never as an empty gap.</summary>
    public static string SafeValue(string value) => string.IsNullOrEmpty(value) ? "-" : value;

    public void OnMusicToggled(bool isOn)
    {
        if (_audio != null) _audio.MusicEnabled = isOn;
    }

    public void OnSfxToggled(bool isOn)
    {
        if (_audio == null) return;
        _audio.SfxEnabled = isOn;

        // Turning the sounds back on gives a click, so the player hears they are on
        if (isOn) _audio.PlaySfx(SfxId.ButtonClick);
    }

    public void OnCreditsClicked()
    {
        if (creditsPopupPrefab == null)
        {
            Debug.LogWarning("[SettingsPopup] No CreditsPopup prefab assigned.");
            return;
        }

        Service?.Open(creditsPopupPrefab);
    }

    /// <summary>Opens the privacy policy in the browser, at the section of the language on screen.</summary>
    public void OnPrivacyClicked()
    {
        _urlOpener ??= new UnityUrlOpener();
        _urlOpener.Open(LegalLinks.PrivacyPolicyUrl(_localization?.CurrentLanguage));
    }
}
