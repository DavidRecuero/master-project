using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Privacy preferences from Settings: the analytics switch shows and changes the saved consent.</summary>
public class PrivacySettingsPopupTests
{
    private GameObject _serviceGo;
    private PopupService _service;
    private PrivacySettingsPopup _prefab;
    private Toggle _prefabToggle;
    private ConsentService _consent;
    private FakeUrlOpener _browser;
    private FakeLocalizationService _localization;
    private readonly List<bool> _changes = new List<bool>();

    [SetUp]
    public void SetUp()
    {
        _serviceGo = new GameObject("PopupService");
        _service = _serviceGo.AddComponent<PopupService>();

        _prefab = PopupTestUtils.MakePrefab<PrivacySettingsPopup>("PrivacySettingsPopup");
        var toggleGo = new GameObject("Toggle", typeof(RectTransform));
        toggleGo.transform.SetParent(_prefab.transform, false);
        _prefabToggle = toggleGo.AddComponent<Toggle>();
        PopupTestUtils.SetField(_prefab, "analyticsToggle", _prefabToggle);

        _consent = new ConsentService(new ConsentStore(new FakeStorageProvider()), new FakeClock());
        _browser = new FakeUrlOpener();
        _localization = new FakeLocalizationService();

        _changes.Clear();
        GameEvents.OnConsentChanged += OnChanged;
    }

    [TearDown]
    public void TearDown()
    {
        GameEvents.OnConsentChanged -= OnChanged;
        Object.DestroyImmediate(_serviceGo);
        Object.DestroyImmediate(_prefab.gameObject);
    }

    private void OnChanged(ConsentPurpose purpose, bool granted) => _changes.Add(granted);

    private PrivacySettingsPopup Open()
    {
        PrivacySettingsPopup popup = _service.Open(_prefab);
        popup.Initialize(_consent, _browser, _localization);
        return popup;
    }

    private static Toggle ToggleOf(PrivacySettingsPopup popup) => popup.GetComponentInChildren<Toggle>();

    [TestCase(true)]
    [TestCase(false)]
    public void Opening_ShowsTheSavedAnswer(bool granted)
    {
        _consent.SetAll(granted);
        _changes.Clear();

        PrivacySettingsPopup popup = Open();

        Assert.AreEqual(granted, ToggleOf(popup).isOn);
        Assert.IsEmpty(_changes, "Showing the saved state must not save it again");
    }

    [Test]
    public void SwitchOff_WithdrawsConsent()
    {
        _consent.SetAll(true);
        _changes.Clear();
        PrivacySettingsPopup popup = Open();

        popup.OnAnalyticsToggled(false);

        Assert.IsFalse(_consent.IsGranted(ConsentPurpose.Analytics));
        CollectionAssert.AreEqual(new[] { false }, _changes);
    }

    [Test]
    public void SwitchOn_GivesConsent()
    {
        _consent.SetAll(false);
        PrivacySettingsPopup popup = Open();

        popup.OnAnalyticsToggled(true);

        Assert.IsTrue(_consent.IsGranted(ConsentPurpose.Analytics));
        CollectionAssert.AreEqual(new[] { true }, _changes);
    }

    [Test]
    public void CanBeClosedLikeAnyPopup()
    {
        PrivacySettingsPopup popup = Open();

        popup.Close();

        Assert.AreEqual(0, _service.Count);
    }

    [TestCase("en")]
    [TestCase("es")]
    [TestCase("ca")]
    public void PolicyButton_OpensThePolicyInTheLanguageOnScreen(string language)
    {
        _localization.CurrentLanguage = language;
        PrivacySettingsPopup popup = Open();

        popup.OnPolicyClicked();

        Assert.AreEqual(LegalLinks.PrivacyPolicyUrl(language), _browser.Opened[0]);
    }

    [Test]
    public void SettingsPrivacyButton_OpensThePreferencesOnTop()
    {
        var settingsPrefab = PopupTestUtils.MakePrefab<SettingsPopup>("SettingsPopup");
        PopupTestUtils.SetField(settingsPrefab, "privacySettingsPopupPrefab", _prefab);
        try
        {
            SettingsPopup settings = _service.Open(settingsPrefab);
            settings.Initialize(new FakeAudioService(), localization: _localization, urlOpener: _browser);

            settings.OnPrivacyClicked();

            Assert.AreEqual(2, _service.Count);
            Assert.IsInstanceOf<PrivacySettingsPopup>(_service.Top);
            Assert.IsEmpty(_browser.Opened, "With the preferences popup, the browser is opened from inside it");
        }
        finally
        {
            Object.DestroyImmediate(settingsPrefab.gameObject);
        }
    }
}
