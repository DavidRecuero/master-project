using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BootManagerTests
{
    [UnityTest]
    public IEnumerator StartupSequence_CompletesAndLoadsMainMenu()
    {
        GameObject bootGO = new GameObject();
        BootManager bootManager = bootGO.AddComponent<BootManager>();
        FakeSceneLoader fakeSceneLoader = new FakeSceneLoader();
        FakeAuthProvider fakeAuthProvider = new FakeAuthProvider();

        bootManager.Initialize(fakeSceneLoader, fakeAuthProvider);

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

        bootManager.Initialize(fakeSceneLoader, fakeAuthProvider);

        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(1, fakeSceneLoader.LoadedSceneIndex, "Boot must never block on a failed sign-in.");

        Object.Destroy(bootGO);
    }
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