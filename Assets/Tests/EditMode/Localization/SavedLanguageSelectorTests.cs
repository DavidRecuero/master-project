using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class SavedLanguageSelectorTests
{
    private FakeLocalesProvider _locales;

    [SetUp]
    public void SetUp()
    {
        _locales = new FakeLocalesProvider();
        _locales.AddLocale(Locale.CreateLocale("en"));
        _locales.AddLocale(Locale.CreateLocale("es"));
        _locales.AddLocale(Locale.CreateLocale("ca"));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Locale locale in _locales.Locales) Object.DestroyImmediate(locale);
    }

    [Test]
    public void SavedLanguage_StartsTheGameInIt()
    {
        Locale locale = SavedLanguageSelector.Resolve("ca", _locales);

        Assert.IsNotNull(locale);
        Assert.AreEqual("ca", locale.Identifier.Code);
    }

    [TestCase(null)]
    [TestCase("")]
    public void NoSavedLanguage_LetsTheDeviceDecide(string saved)
    {
        Assert.IsNull(SavedLanguageSelector.Resolve(saved, _locales), "null passes the decision to the next selector");
    }

    [Test]
    public void SavedLanguageNoLongerInTheGame_LetsTheDeviceDecide()
    {
        Assert.IsNull(SavedLanguageSelector.Resolve("fr", _locales));
    }

    private class FakeLocalesProvider : ILocalesProvider
    {
        public List<Locale> Locales { get; } = new List<Locale>();

        public Locale GetLocale(LocaleIdentifier id) => Locales.Find(l => l.Identifier.Code == id.Code);

        public void AddLocale(Locale locale) => Locales.Add(locale);

        public bool RemoveLocale(Locale locale) => Locales.Remove(locale);
    }
}
