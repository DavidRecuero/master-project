using NUnit.Framework;
using UnityEngine;

public class UiHapticTests
{
    private GameObject _go;
    private UiHaptic _uiHaptic;
    private FakeHapticsService _haptics;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Button");
        _uiHaptic = _go.AddComponent<UiHaptic>();
        _haptics = new FakeHapticsService();
        _uiHaptic.Initialize(_haptics);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void Default_IsTheSmallestTick()
    {
        Assert.AreEqual(HapticType.Selection, _uiHaptic.Haptic);
    }

    [Test]
    public void Play_SendsItsHapticToTheService()
    {
        _uiHaptic.Play();

        Assert.AreEqual(1, _haptics.Played.Count);
        Assert.AreEqual(HapticType.Selection, _haptics.Last);
    }

    [Test]
    public void ButtonPrefabs_HaveTheTick()
    {
        // The base prefabs every UI button is built from
        string[] prefabs =
        {
            "Assets/_Project/Prefab/UI/Common/PrimaryButton.prefab",
            "Assets/_Project/Prefab/UI/Common/SecondaryButton.prefab",
            "Assets/_Project/Prefab/UI/Common/IconButton.prefab",
            "Assets/_Project/Prefab/UI/Common/CloseButton.prefab",
            "Assets/_Project/Prefab/UI/Common/CoinsPill.prefab",
            "Assets/_Project/Prefab/UI/Common/LivesPill.prefab"
        };

        foreach (string path in prefabs)
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"Prefab not found: {path}");
            Assert.IsNotNull(prefab.GetComponent<UiHaptic>(), $"{prefab.name} has no UiHaptic");
        }
    }
}
