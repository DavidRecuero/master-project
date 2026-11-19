using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// The Remote Config step of the Boot: downloads only when signed in, never blocks the menu, locks the values for
/// the session and shares them with the game. Also the remote kill switch of analytics.
/// </summary>
public class BootRemoteConfigTests
{
    private GameObject _bootGO;
    private BootManager _boot;
    private FakeSceneLoader _sceneLoader;
    private BootFakeRemoteConfig _remote;
    private BootFakeAnalytics _analytics;
    private BootFakeConsent _consent;

    [SetUp]
    public void SetUp()
    {
        PlayerSession.Reset();
        BackendServices.Reset();
        RemoteConfigServices.Reset();

        _sceneLoader = new FakeSceneLoader();
        _remote = new BootFakeRemoteConfig();
        _analytics = new BootFakeAnalytics();
        _consent = new BootFakeConsent();
        _consent.SetAll(true);
    }

    [TearDown]
    public void TearDown()
    {
        if (_bootGO != null) Object.Destroy(_bootGO);
        var reconnector = Object.FindFirstObjectByType<CloudReconnector>();
        if (reconnector != null) Object.Destroy(reconnector.gameObject);
        PlayerSession.Reset();
        BackendServices.Reset();
        RemoteConfigServices.Reset();
        AnalyticsServices.Set(null);
    }

    /// <summary>Creates the Boot and injects everything in the same frame, before its Start runs.</summary>
    private void Init(bool cloud, bool remoteEnabled = true, float timeout = 1f)
    {
        _bootGO = new GameObject("Boot");
        _boot = _bootGO.AddComponent<BootManager>();
        _boot.Initialize(_sceneLoader, new FakeAuthProvider(), new FakeBootLocalization());
        if (cloud)
            _boot.InitializeCloud(new BootFakeBackendAuth(), new BootFakeCloudStore(), new BootFakeProfiles(), 1f);
        else
            _boot.InitializeCloud(null, null, null, enabled: false);
        _boot.InitializeConsent(_consent, new BootFakePopups());
        _boot.InitializeAnalytics(_analytics);
        _boot.InitializeRemoteConfig(_remote, remoteEnabled, timeout);
    }

    [UnityTest]
    public IEnumerator SignedIn_DownloadsAndShares_BeforeTheMenu()
    {
        Init(cloud: true);

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(1, _remote.FetchCalls);
        Assert.AreSame(_remote, RemoteConfigServices.Current);
        Assert.IsTrue(_remote.ValuesInUse, "Values are locked once the game starts");
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator CloudOff_DoesNotDownload_ButSharesTheSavedValues()
    {
        Init(cloud: false);

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(0, _remote.FetchCalls);
        Assert.AreSame(_remote, RemoteConfigServices.Current);
        Assert.IsTrue(_remote.ValuesInUse);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator SlowServer_NeverBlocksTheMenu()
    {
        _remote.NeverAnswer = true;
        Init(cloud: true, timeout: 0.2f);

        yield return new WaitForSeconds(0.8f);

        Assert.AreEqual(1, _remote.FetchCalls);
        Assert.IsTrue(_remote.ValuesInUse, "A late answer must not change the values of this session");
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator KillSwitch_UsesOnlyTheLocalAssets()
    {
        Init(cloud: true, remoteEnabled: false);

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(0, _remote.FetchCalls);
        Assert.AreNotSame(_remote, RemoteConfigServices.Current);
        Assert.AreEqual(0, RemoteConfigServices.Current.Values.Count);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator RemoteAnalyticsKillSwitch_NeverStartsAnalytics()
    {
        _remote.Values = new RemoteValues(new System.Collections.Generic.Dictionary<string, string>
        {
            { RemoteKeys.AnalyticsEnabled, "false" }
        });
        Init(cloud: false);

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(0, _analytics.StartCalls);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator SegmentAndExperiment_ReachTheAnalytics()
    {
        _remote.Values = new RemoteValues(new System.Collections.Generic.Dictionary<string, string>
        {
            { RemoteKeys.Segment, "stuck" },
            { RemoteKeys.Experiment, "b" },
            { RemoteKeys.Lifecycle, "mature" }
        });
        Init(cloud: false);

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(1, _analytics.StartCalls);
        Assert.AreEqual(PlayerSegments.Stuck, _analytics.Segment);
        Assert.AreEqual(PlayerSegments.VariantB, _analytics.Experiment);
        Assert.AreEqual(PlayerSegments.Mature, _analytics.Lifecycle);
    }
}

/// <summary>Remote settings with fixed values. Counts the downloads; can never answer.</summary>
public class BootFakeRemoteConfig : IRemoteConfigService
{
    public RemoteValues Values { get; set; } = RemoteValues.Empty;
    public RemoteConfigSource Source { get; set; } = RemoteConfigSource.Defaults;
    public bool ValuesInUse { get; set; }
    public bool NeverAnswer;
    public int FetchCalls;

    public Task<bool> FetchAsync()
    {
        FetchCalls++;
        if (NeverAnswer) return new TaskCompletionSource<bool>().Task;
        Source = RemoteConfigSource.Remote;
        return Task.FromResult(true);
    }
}
