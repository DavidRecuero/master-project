using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// First-launch privacy popup (GDPR opt-in): explains what analytics are used for and lets the player accept or
/// reject. Plain on purpose, not styled like the game's menus: Accept and Reject look the same and have the same
/// size, so the player isn't pushed towards either (the AEPD fines "dark patterns").
/// It can't be left without answering: no X, and the dim and the back button do nothing. Once the player answers,
/// the choice is saved (IConsentService) and the popup closes. It can be changed later from Settings.
/// The texts are fixed (LocalizeStringEvent in the prefab), so nothing is translated from here.
/// </summary>
public class ConsentPopup : PopupView
{
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [Tooltip("Opens LegalLinks.PrivacyPolicy in the browser, in the language on screen")]
    [SerializeField] private Button policyButton;

    private IConsentService _consent;
    private IUrlOpener _urlOpener;
    private ILocalizationService _localization;

    /// <summary>True once the player accepted or rejected.</summary>
    public bool Answered { get; private set; }

    /// <summary>Only an answer closes it.</summary>
    public override bool CanClose => Answered;

    /// <summary>Optional injection (tests). Without it, the shared ConsentServices, the device browser and the shared UnityLocalizationService.</summary>
    public void Initialize(IConsentService consent, IUrlOpener urlOpener = null, ILocalizationService localization = null)
    {
        _consent = consent;
        if (urlOpener != null) _urlOpener = urlOpener;
        if (localization != null) _localization = localization;
    }

    private void Awake()
    {
        if (acceptButton != null) acceptButton.onClick.AddListener(OnAcceptClicked);
        if (rejectButton != null) rejectButton.onClick.AddListener(OnRejectClicked);
        if (policyButton != null) policyButton.onClick.AddListener(OnPolicyClicked);
    }

    private void OnDestroy()
    {
        if (acceptButton != null) acceptButton.onClick.RemoveListener(OnAcceptClicked);
        if (rejectButton != null) rejectButton.onClick.RemoveListener(OnRejectClicked);
        if (policyButton != null) policyButton.onClick.RemoveListener(OnPolicyClicked);
    }

    protected override void OnOpened()
    {
        _consent ??= ConsentServices.Current;
        _localization ??= UnityLocalizationService.Instance;
    }

    public void OnAcceptClicked() => Answer(true);

    public void OnRejectClicked() => Answer(false);

    /// <summary>Opens the privacy policy in the browser, at the section of the language on screen. The popup stays open.</summary>
    public void OnPolicyClicked()
    {
        _urlOpener ??= new UnityUrlOpener();
        _urlOpener.Open(LegalLinks.PrivacyPolicyUrl(_localization?.CurrentLanguage));
    }

    private void Answer(bool granted)
    {
        if (Answered) return;   // double tap while closing

        _consent ??= ConsentServices.Current;
        _consent.SetAll(granted);
        Answered = true;

        Debug.Log($"[Privacy] Consent {(granted ? "granted" : "rejected")} (version {ConsentService.CurrentVersion})");
        Close();
    }
}
