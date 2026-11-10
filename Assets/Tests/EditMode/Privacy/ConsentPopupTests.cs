using NUnit.Framework;
using UnityEngine;

/// <summary>First-launch consent popup: accept or reject saves the answer and closes; nothing else closes it.</summary>
public class ConsentPopupTests
{
    private GameObject _serviceGo;
    private PopupService _service;
    private ConsentPopup _prefab;
    private ConsentService _consent;
    private FakeUrlOpener _browser;
    private FakeLocalizationService _localization;

    [SetUp]
    public void SetUp()
    {
        _serviceGo = new GameObject("PopupService");
        _service = _serviceGo.AddComponent<PopupService>();
        _prefab = PopupTestUtils.MakePrefab<ConsentPopup>("ConsentPopup");
        _consent = new ConsentService(new ConsentStore(new FakeStorageProvider()), new FakeClock());
        _browser = new FakeUrlOpener();
        _localization = new FakeLocalizationService();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_serviceGo);
        Object.DestroyImmediate(_prefab.gameObject);
    }

    private ConsentPopup Open()
    {
        ConsentPopup popup = _service.Open(_prefab);
        popup.Initialize(_consent, _browser, _localization);
        return popup;
    }

    [Test]
    public void Accept_GrantsAndCloses()
    {
        ConsentPopup popup = Open();

        popup.OnAcceptClicked();

        Assert.IsTrue(_consent.IsGranted(ConsentPurpose.Analytics));
        Assert.IsFalse(_consent.NeedsPrompt);
        Assert.AreEqual(0, _service.Count);
    }

    [Test]
    public void Reject_DeniesAndCloses()
    {
        ConsentPopup popup = Open();

        popup.OnRejectClicked();

        Assert.AreEqual(ConsentStatus.Denied, _consent.GetStatus(ConsentPurpose.Analytics));
        Assert.IsFalse(_consent.NeedsPrompt);
        Assert.AreEqual(0, _service.Count);
    }

    [Test]
    public void BeforeAnswering_CannotBeClosed()
    {
        ConsentPopup popup = Open();

        Assert.IsFalse(popup.CanClose);

        popup.Close();                              // X / dim
        Assert.AreEqual(1, _service.Count);

        Assert.IsTrue(_service.HandleBack());       // back button: handled, but stays open
        Assert.AreEqual(1, _service.Count);
        Assert.IsTrue(_consent.NeedsPrompt);
    }

    [Test]
    public void DoubleTap_SavesOnlyTheFirstAnswer()
    {
        ConsentPopup popup = Open();

        popup.OnAcceptClicked();
        popup.OnRejectClicked();

        Assert.IsTrue(_consent.IsGranted(ConsentPurpose.Analytics));
    }

    [TestCase("en")]
    [TestCase("es")]
    [TestCase("ca")]
    public void PolicyButton_OpensThePolicyInTheLanguageOnScreenAndStaysOpen(string language)
    {
        _localization.CurrentLanguage = language;
        ConsentPopup popup = Open();

        popup.OnPolicyClicked();

        Assert.AreEqual(1, _browser.Opened.Count);
        Assert.AreEqual(LegalLinks.PrivacyPolicyUrl(language), _browser.Opened[0]);
        Assert.AreEqual(1, _service.Count);
        Assert.IsFalse(popup.Answered);
    }
}
