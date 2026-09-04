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

        bootManager.Initialize(fakeSceneLoader);

        yield return new WaitForSeconds(0.6f);

        Assert.AreEqual(1, fakeSceneLoader.LoadedSceneIndex);

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