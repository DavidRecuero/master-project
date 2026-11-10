using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Privacy preferences, opened from Settings: the analytics switch (on = consent given) and the privacy policy.
/// The switch shows the saved answer when the popup opens, and each change is saved at once through
/// IConsentService, which raises GameEvents.OnConsentChanged so analytics start or stop. Withdrawing is as easy as
/// giving consent (GDPR): one tap, no confirmation.
/// Closes like any other popup (X, dim, back button).
/// </summary>
public class PrivacySettingsPopup : PopupView
{
    [Tooltip("Analytics switch (Toggle with SwitchToggleView)")]
    [SerializeField] private Toggle analyticsToggle;
    [Tooltip("Opens LegalLinks.PrivacyPolicy in the browser, in the language on screen")]
    [SerializeField] private Button policyButton;

    private IConsentService _consent;
    private IUrlOpener _urlOpener;
    private ILocalizationService _localization;

    /// <summary>Optional injection (tests). Without it, the shared ConsentServices, the device browser and the shared UnityLocalizationService.</summary>
    public void Initialize(IConsentService consent, IUrlOpener urlOpener = null, ILocalizationService localization = null)
    {
        _consent = consent;
        if (urlOpener != null) _urlOpener = urlOpener;
        if (localization != null) _localization = localization;
        Refresh();
    }

    private void Awake()
    {
        if (analyticsToggle != null) analyticsToggle.onValueChanged.AddListener(OnAnalyticsToggled);
        if (policyButton != null) policyButton.onClick.AddListener(OnPolicyClicked);
    }

    private void OnDestroy()
    {
        if (analyticsToggle != null) analyticsToggle.onValueChanged.RemoveListener(OnAnalyticsToggled);
        if (policyButton != null) policyButton.onClick.RemoveListener(OnPolicyClicked);
    }

    protected override void OnOpened()
    {
        _consent ??= ConsentServices.Current;
        _localization ??= UnityLocalizationService.Instance;
        Refresh();
    }

    /// <summary>Puts the switch on the saved answer without firing its event (that would save it again).</summary>
    private void Refresh()
    {
        if (_consent == null || analyticsToggle == null) return;
        analyticsToggle.SetIsOnWithoutNotify(_consent.IsGranted(ConsentPurpose.Analytics));
    }

    public void OnAnalyticsToggled(bool isOn)
    {
        _consent ??= ConsentServices.Current;
        _consent.Set(ConsentPurpose.Analytics, isOn);
        Debug.Log($"[Privacy] Analytics {(isOn ? "allowed" : "not allowed")} from Settings");
    }

    /// <summary>Opens the privacy policy in the browser, at the section of the language on screen.</summary>
    public void OnPolicyClicked()
    {
        _urlOpener ??= new UnityUrlOpener();
        _urlOpener.Open(LegalLinks.PrivacyPolicyUrl(_localization?.CurrentLanguage));
    }
}
