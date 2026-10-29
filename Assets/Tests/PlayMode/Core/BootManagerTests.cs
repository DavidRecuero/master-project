using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BootManagerTests
{
    [SetUp]
    public void SetUp() => PlayerSession.Reset();

    [TearDown]
    public void TearDown() => PlayerSession.Reset();

    [UnityTest]
    public IEnumerator StartupSequence_PublishesNameAndAvatar_WhenSignedIn()
    {
        GameObject bootGO = new GameObject();
        BootManager bootManager = bootGO.AddComponent<BootManager>();
        var fakeAuthProvider = new FakeAuthProvider
        {
            DisplayName = "Ana",
            AvatarUrl = "https://example.com/a.png"
        };

        bootManager.Initialize(new FakeSceneLoader(), fakeAuthProvider, new FakeBootLocalization());

        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual("Ana", PlayerSession.DisplayName);
        Assert.AreEqual("https://example.com/a.png", PlayerSession.AvatarUrl);

        Object.Destroy(bootGO);
    }

    [UnityTest]
    public IEnumerator StartupSequence_LeavesSessionEmpty_WhenSignInFails()
    {
        GameObject bootGO = new GameObject();
        BootManager bootManager = bootGO.AddComponent<BootManager>();
        var fakeAuthProvider = new FakeAuthProvider
        {
            DisplayName = "Ana",
            ResultToReturn = AuthResult.NoNetwork
        };

        bootManager.Initialize(new FakeSceneLoader(), fakeAuthProvider, new FakeBootLocalization());

        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual("", PlayerSession.DisplayName);

        Object.Destroy(bootGO);
    }

    [UnityTest]
    public IEnumerator StartupSequence_CompletesAndLoadsMainMenu()
    {
        GameObject bootGO = new GameObject();
        BootManager bootManager = bootGO.AddComponent<BootManager>();
        FakeSceneLoader fakeSceneLoader = new FakeSceneLoader();
        FakeAuthProvider fakeAuthProvider = new FakeAuthProvider();

        bootManager.Initialize(fakeSceneLoader, fakeAuthProvider, new FakeBootLocalization());

        yield return new WaitForSeconds(0.2f);

        Assert.IsTrue(fakeAuthProvider.SignInCalled);
        Assert.AreEqual(1, fakeSceneLoader.LoadedSceneIndex);

        Object.Destroy(bootGO);
    }

    [UnityTest]
    public IEnumerator StartupSequence_LoadsMainMenu_EvenWhenSignInFails()
    {
        GameObject bootGO = new GameObject();
        BootManager bootManager = bootGO.AddComponent<BootManager>();
        FakeSceneLoader fakeSceneLoader = new FakeSceneLoader();
        FakeAuthProvider fakeAuthProvider = new FakeAuthProvider { ResultToReturn = AuthResult.NoNetwork };

        bootManager.Initialize(fakeSceneLoader, fakeAuthProvider, new FakeBootLocalization());

        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(1, fakeSceneLoader.LoadedSceneIndex, "Boot must never block on a failed sign-in.");

        Object.Destroy(bootGO);
    }

    [UnityTest]
    public IEnumerator StartupSequence_WaitsForTheLanguageTables_BeforeLoadingTheMenu()
    {
        GameObject bootGO = new GameObject();
        BootManager bootManager = bootGO.AddComponent<BootManager>();
        FakeSceneLoader fakeSceneLoader = new FakeSceneLoader();
        var localization = new FakeBootLocalization { CompleteRightAway = false };

        bootManager.Initialize(fakeSceneLoader, new FakeAuthProvider(), localization);

        yield return new WaitForSeconds(0.2f);

        Assert.IsTrue(localization.InitializeCalled, "Tables start loading at boot");
        Assert.AreEqual(-1, fakeSceneLoader.LoadedSceneIndex, "The menu would open with untranslated labels");

        localization.Complete();
        yield return null;
        yield return null;

        Assert.AreEqual(1, fakeSceneLoader.LoadedSceneIndex);

        Object.Destroy(bootGO);
    }
}

/// <summary>Language service for the boot tests: ready right away, or when the test says so.</summary>
public class FakeBootLocalization : ILocalizationService
{
    public bool CompleteRightAway { get; set; } = true;
    public bool InitializeCalled { get; private set; }
    private System.Action _onReady;

    public bool IsReady { get; private set; }
    public System.Collections.Generic.IReadOnlyList<LanguageInfo> Languages => new LanguageInfo[0];
    public string CurrentLanguage => "en";
    public bool FollowsDevice => true;
    public event System.Action LanguageChanged { add { } remove { } }

    public void Initialize(System.Action onReady)
    {
        InitializeCalled = true;
        _onReady = onReady;
        if (CompleteRightAway) Complete();
    }

    public void Complete()
    {
        IsReady = true;
        _onReady?.Invoke();
        _onReady = null;
    }

    public void SetLanguage(string code) { }
    public void UseDeviceLanguage() { }
}

public class FakeSceneLoader : ISceneLoader
{
    public int LoadedSceneIndex { get; private set; } = -1;
    public bool ReloadCalled { get; private set; }

    public void LoadScene(int sceneIndex) => LoadedSceneIndex = sceneIndex;
    public void ReloadCurrentScene() => ReloadCalled = true;
}

public class FakeAuthProvider : IAuthProvider
{
    public bool IsSignedIn { get; set; }
    public string PlayerId { get; set; } = "fake-player-id";
    public string DisplayName { get; set; } = "Fake Player";
    public AuthResult ResultToReturn { get; set; } = AuthResult.Success;
    public bool SignInCalled { get; private set; }
    public string AvatarUrl { get; set; } = "";

    public void SignIn(System.Action<AuthResult> onComplete)
    {
        SignInCalled = true;
        IsSignedIn = ResultToReturn == AuthResult.Success;
        onComplete?.Invoke(ResultToReturn);
    }
}