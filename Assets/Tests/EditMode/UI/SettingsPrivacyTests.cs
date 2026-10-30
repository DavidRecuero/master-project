using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Privacy policy button of the Settings popup. Google Play requires the policy to be reachable from
/// inside the game; it opens in the browser, at the section of the language on screen.
/// </summary>
public class SettingsPrivacyTests
{
    private GameObject _root;
    private SettingsPopup _settings;
    private FakeLocalizationService _localization;
    private FakeUrlOpener _browser;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("SettingsPopup", typeof(RectTransform));
        _settings = _root.AddComponent<SettingsPopup>();
        _localization = new FakeLocalizationService();
        _browser = new FakeUrlOpener();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
    }

    [TestCase("en")]
    [TestCase("es")]
    [TestCase("ca")]
    public void PrivacyButton_OpensThePolicyInTheLanguageOnScreen(string language)
    {
        _localization.CurrentLanguage = language;
        _settings.Initialize(new FakeAudioService(), localization: _localization, urlOpener: _browser);

        _settings.OnPrivacyClicked();

        Assert.AreEqual(1, _browser.Opened.Count);
        Assert.AreEqual(LegalLinks.PrivacyPolicyUrl(language), _browser.Opened[0]);
    }

    [Test]
    public void PrivacyButton_FollowsALanguageChange()
    {
        _settings.Initialize(new FakeAudioService(), localization: _localization, urlOpener: _browser);

        _localization.SetLanguage("ca");
        _settings.OnPrivacyClicked();

        Assert.AreEqual(LegalLinks.PrivacyPolicy + "#ca", _browser.Opened[0]);
    }

    [Test]
    public void PrivacyButton_WithoutLanguageYet_StillOpensThePolicy()
    {
        _localization.CurrentLanguage = null;
        _settings.Initialize(new FakeAudioService(), localization: _localization, urlOpener: _browser);

        _settings.OnPrivacyClicked();

        Assert.AreEqual(LegalLinks.PrivacyPolicy, _browser.Opened[0]);
    }
}

/// <summary>Records the pages the code under test asked to open. Opens nothing.</summary>
public class FakeUrlOpener : IUrlOpener
{
    public readonly List<string> Opened = new List<string>();

    public void Open(string url) => Opened.Add(url);
}
