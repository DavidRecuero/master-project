using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>The Boot asks for privacy consent before the menu, only when an answer is missing.</summary>
public class BootConsentTests
{
    private GameObject _bootGo;
    private GameObject _prefabGo;
    private BootManager _boot;
    private ConsentPopup _prefab;
    private FakeSceneLoader _sceneLoader;
    private BootFakeConsent _consent;
    private BootFakePopups _popups;

    [SetUp]
    public void SetUp()
    {
        PlayerSession.Reset();
        _bootGo = new GameObject("Boot");
        _boot = _bootGo.AddComponent<BootManager>();
        _prefabGo = new GameObject("ConsentPopup", typeof(RectTransform));
        _prefab = _prefabGo.AddComponent<ConsentPopup>();
        _sceneLoader = new FakeSceneLoader();
        _consent = new BootFakeConsent();
        _popups = new BootFakePopups();

        _boot.Initialize(_sceneLoader, new FakeAuthProvider(), new FakeBootLocalization());
        _boot.InitializeCloud(null, null, null, enabled: false);
        _boot.InitializeConsent(_consent, _popups, _prefab);
    }

    [TearDown]
    public void TearDown()
    {
        if (_bootGo != null) Object.Destroy(_bootGo);
        if (_prefabGo != null) Object.Destroy(_prefabGo);
        PlayerSession.Reset();
    }

    [UnityTest]
    public IEnumerator NoAnswer_ShowsThePopupAndWaitsBeforeTheMenu()
    {
        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(1, _popups.Opened.Count);
        Assert.AreEqual(-1, _sceneLoader.LoadedSceneIndex, "The menu must wait for the answer");
    }

    [UnityTest]
    public IEnumerator Accept_SavesItAndLoadsTheMenu()
    {
        yield return new WaitForSeconds(0.3f);

        _prefab.OnAcceptClicked();
        yield return null;
        yield return null;

        Assert.AreEqual(true, _consent.Answer);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator Reject_SavesItAndLoadsTheMenu()
    {
        yield return new WaitForSeconds(0.3f);

        _prefab.OnRejectClicked();
        yield return null;
        yield return null;

        Assert.AreEqual(false, _consent.Answer);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator AlreadyAnswered_LoadsTheMenuWithoutAsking()
    {
        _consent.SetAll(false);

        yield return new WaitForSeconds(0.3f);

        Assert.IsEmpty(_popups.Opened);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator NoPopupService_LoadsTheMenuAnyway()
    {
        _boot.InitializeConsent(_consent, null, _prefab);

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }
}

/// <summary>Consent in memory: starts with no answer.</summary>
public class BootFakeConsent : IConsentService
{
    public bool? Answer;
    public bool Granted => Answer == true;

    public bool NeedsPrompt => Answer == null;
    public ConsentStatus GetStatus(ConsentPurpose purpose) =>
        Answer == null ? ConsentStatus.Unknown : (Answer.Value ? ConsentStatus.Granted : ConsentStatus.Denied);
    public bool IsGranted(ConsentPurpose purpose) => Granted;
    public DateTime? DecisionUtc => null;
    public void SetAll(bool granted) => Answer = granted;
    public void Set(ConsentPurpose purpose, bool granted) => Answer = granted;
    public void Reset() => Answer = null;
}

/// <summary>Records the popups asked for and hands back the prefab object itself (it is already in the scene).</summary>
public class BootFakePopups : IPopupService
{
    public readonly List<PopupView> Opened = new List<PopupView>();

    public T Open<T>(T prefab) where T : PopupView
    {
        Opened.Add(prefab);
        return prefab;
    }

    public void Close(PopupView popup) => Opened.Remove(popup);
    public bool CloseTop() => false;
    public void CloseAll() => Opened.Clear();
    public PopupView Top => Opened.Count > 0 ? Opened[Opened.Count - 1] : null;
    public int Count => Opened.Count;
}
