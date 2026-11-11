using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>The Boot starts analytics right after the consent step, and shares the service with the game.</summary>
public class BootAnalyticsTests
{
    private GameObject _bootGo;
    private GameObject _prefabGo;
    private BootManager _boot;
    private ConsentPopup _prefab;
    private FakeSceneLoader _sceneLoader;
    private BootFakeConsent _consent;
    private BootFakeAnalytics _analytics;

    [SetUp]
    public void SetUp()
    {
        _bootGo = new GameObject("Boot");
        _boot = _bootGo.AddComponent<BootManager>();
        _prefabGo = new GameObject("ConsentPopup", typeof(RectTransform));
        _prefab = _prefabGo.AddComponent<ConsentPopup>();
        _sceneLoader = new FakeSceneLoader();
        _consent = new BootFakeConsent();
        _analytics = new BootFakeAnalytics();

        _boot.Initialize(_sceneLoader, new FakeAuthProvider(), new FakeBootLocalization());
        _boot.InitializeCloud(null, null, null, enabled: false);
        _boot.InitializeConsent(_consent, new BootFakePopups(), _prefab);
    }

    [TearDown]
    public void TearDown()
    {
        AnalyticsServices.Set(null);
        if (_bootGo != null) Object.Destroy(_bootGo);
        if (_prefabGo != null) Object.Destroy(_prefabGo);
    }

    [UnityTest]
    public IEnumerator AlreadyAnswered_StartsAnalyticsBeforeTheMenu()
    {
        _consent.SetAll(true);
        _boot.InitializeAnalytics(_analytics);

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(1, _analytics.StartCalls);
        Assert.AreSame(_analytics, AnalyticsServices.Current);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator FirstLaunch_StartsAnalyticsOnlyAfterTheAnswer()
    {
        _boot.InitializeAnalytics(_analytics);

        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(0, _analytics.StartCalls, "Nothing may start while the popup waits");

        _prefab.OnAcceptClicked();
        yield return null;
        yield return null;

        Assert.AreEqual(1, _analytics.StartCalls);
    }

    [UnityTest]
    public IEnumerator KillSwitch_NeverStartsAnalytics()
    {
        _consent.SetAll(true);
        _boot.InitializeAnalytics(_analytics, enabled: false);

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(0, _analytics.StartCalls);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }
}

/// <summary>Counts the starts. Sends nothing.</summary>
public class BootFakeAnalytics : IAnalyticsService
{
    public int StartCalls;
    public bool IsActive => StartCalls > 0;
    public void Start() => StartCalls++;
    public void Stop() { }
    public void SetAppPaused(bool paused) { }
    public void Progression(AnalyticsProgressionStatus status, string progression01, string progression02 = null, int? score = null) { }
    public void Resource(AnalyticsResourceFlow flow, string currency, float amount, string itemType, string itemId) { }
    public void Design(string eventId, float? value = null) { }
}
