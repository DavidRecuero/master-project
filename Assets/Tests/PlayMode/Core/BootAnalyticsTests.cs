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
    private BootFakePopups _popups;

    // The Boot now also loads the levels (Addressables) before the menu: the first load in the editor can take
    // longer than a fixed wait, so the tests wait for the Boot to get there, with a generous limit
    private const float MaxWait = 10f;

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
        _popups = new BootFakePopups();

        _boot.Initialize(_sceneLoader, new FakeAuthProvider(), new FakeBootLocalization());
        _boot.InitializeCloud(null, null, null, enabled: false);
        _boot.InitializeContent(null, enabled: false);                        // No CCD check (network)
        _boot.InitializeRemoteConfig(new BootFakeRemoteConfig(), enabled: false); // No values cached on this PC
        _boot.InitializeConsent(_consent, _popups, _prefab);
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

        yield return WaitForMenu();

        Assert.AreEqual(1, _analytics.StartCalls);
        Assert.AreSame(_analytics, AnalyticsServices.Current);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator FirstLaunch_StartsAnalyticsOnlyAfterTheAnswer()
    {
        _boot.InitializeAnalytics(_analytics);

        yield return WaitUntilOrTimeout(() => _popups.Count > 0);
        Assert.AreEqual(1, _popups.Count, "The consent popup opened");
        Assert.AreEqual(0, _analytics.StartCalls, "Nothing may start while the popup waits");

        _prefab.OnAcceptClicked();
        yield return WaitForMenu();

        Assert.AreEqual(1, _analytics.StartCalls);
    }

    [UnityTest]
    public IEnumerator KillSwitch_NeverStartsAnalytics()
    {
        _consent.SetAll(true);
        _boot.InitializeAnalytics(_analytics, enabled: false);

        yield return WaitForMenu();

        Assert.AreEqual(0, _analytics.StartCalls);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    private IEnumerator WaitForMenu() => WaitUntilOrTimeout(() => _sceneLoader.LoadedSceneIndex >= 0);

    private static IEnumerator WaitUntilOrTimeout(System.Func<bool> condition)
    {
        float limit = Time.realtimeSinceStartup + MaxWait;
        yield return new WaitUntil(() => condition() || Time.realtimeSinceStartup >= limit);
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

    public string Segment, Experiment, Lifecycle;
    public void SetSegment(string segment, string experiment, string lifecycle)
    {
        Segment = segment;
        Experiment = experiment;
        Lifecycle = lifecycle;
    }
}
