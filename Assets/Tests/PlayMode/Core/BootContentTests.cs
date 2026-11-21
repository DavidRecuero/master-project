using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>
/// The remote content step of the Boot: the check starts with the Boot, is waited for before the levels are read,
/// and never blocks the menu (no network, no bucket, slow server, kill switch).
/// </summary>
public class BootContentTests
{
    private GameObject _bootGO;
    private BootManager _boot;
    private FakeSceneLoader _sceneLoader;
    private BootFakeContentUpdater _updater;

    [SetUp]
    public void SetUp()
    {
        PlayerSession.Reset();
        BackendServices.Reset();
        RemoteConfigServices.Reset();

        _sceneLoader = new FakeSceneLoader();
        _updater = new BootFakeContentUpdater();
    }

    [TearDown]
    public void TearDown()
    {
        if (_bootGO != null) Object.Destroy(_bootGO);
        PlayerSession.Reset();
        BackendServices.Reset();
        RemoteConfigServices.Reset();
        AnalyticsServices.Set(null);
    }

    private void Init(bool enabled = true, float timeout = 1f)
    {
        var consent = new BootFakeConsent();
        consent.SetAll(true);

        _bootGO = new GameObject("Boot");
        _boot = _bootGO.AddComponent<BootManager>();
        _boot.Initialize(_sceneLoader, new FakeAuthProvider(), new FakeBootLocalization());
        _boot.InitializeCloud(null, null, null, enabled: false);
        _boot.InitializeConsent(consent, new BootFakePopups());
        _boot.InitializeAnalytics(new BootFakeAnalytics());
        _boot.InitializeRemoteConfig(new BootFakeRemoteConfig(), enabled: false);
        _boot.InitializeContent(_updater, enabled, timeout);
    }

    [UnityTest]
    public IEnumerator NewContent_IsCheckedOnce_ThenTheMenuLoads()
    {
        _updater.Result = new ContentUpdateResult(1, 2048);
        Init();

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(1, _updater.Calls);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator NoNetworkOrNoBucket_NeverBlocksTheMenu()
    {
        _updater.Fail = true;
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[Content\] No content update now"));
        Init();

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator SlowServer_TheMenuLoadsAfterTheTimeLimit()
    {
        _updater.NeverAnswer = true;
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"Content check not done"));
        Init(timeout: 0.2f);

        yield return new WaitForSeconds(0.8f);

        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }

    [UnityTest]
    public IEnumerator KillSwitch_NeverAsksForContent()
    {
        Init(enabled: false);

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(0, _updater.Calls);
        Assert.AreEqual(1, _sceneLoader.LoadedSceneIndex);
    }
}

/// <summary>A content check that answers what the test says, fails, or never answers.</summary>
public class BootFakeContentUpdater : IContentUpdater
{
    public ContentUpdateResult Result;
    public bool Fail;
    public bool NeverAnswer;
    public int Calls;

    public Task<ContentUpdateResult> UpdateAsync()
    {
        Calls++;
        if (NeverAnswer) return new TaskCompletionSource<ContentUpdateResult>().Task;
        if (Fail) return Task.FromException<ContentUpdateResult>(new InvalidOperationException("no network"));
        return Task.FromResult(Result);
    }
}
