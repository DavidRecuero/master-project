using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BoosterManagerTests
{
    private GameObject _boardObject;
    private GameObject _trayObject;
    private GameObject _boosterManagerObject;

    private BoosterManager _boosterManager;
    private FakeBoosterInventory _fakeInventory;
    private FakeUserDataProvider _fakeCurrency;

    [SetUp]
    public void SetUp()
    {
        // TrayManager/BoardManager must exist in the scene BEFORE BoosterManager's Awake runs,
        // since it auto-finds them via FindFirstObjectByType when not explicitly assigned.
        _boardObject = new GameObject("BoardManager");
        BoardManager boardManager = _boardObject.AddComponent<BoardManager>();
        boardManager.Initialize(null, new FakeItemPool(), new FakeCameraController());

        _trayObject = new GameObject("TrayManager");
        TrayManager trayManager = _trayObject.AddComponent<TrayManager>();
        trayManager.Initialize(new FakeItemPool(), new FakeGameStateController(), new FakeCameraController());

        _boosterManagerObject = new GameObject("BoosterManager");
        _boosterManager = _boosterManagerObject.AddComponent<BoosterManager>();

        _fakeInventory = new FakeBoosterInventory();
        _fakeCurrency = new FakeUserDataProvider(); // starts with 100 Coins, see CoreTestFakes.cs
        _boosterManager.Initialize(_fakeInventory, _fakeCurrency);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_boosterManagerObject);
        Object.DestroyImmediate(_trayObject);
        Object.DestroyImmediate(_boardObject);
    }

    private void SetDefinitions(params BoosterDefinition[] definitions)
    {
        typeof(BoosterManager)
            .GetField("boosterDefinitions", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_boosterManager, definitions);
    }

    private static BoosterDefinition MakeDefinition(BoosterType type, int coinPrice, int freeStarterCount = 15)
    {
        var definition = ScriptableObject.CreateInstance<BoosterDefinition>();
        definition.type = type;
        definition.coinPrice = coinPrice;
        definition.freeStarterCount = freeStarterCount;
        return definition;
    }

    [Test]
    public void GetCoinPrice_ReturnsPriceFromMatchingDefinition()
    {
        SetDefinitions(MakeDefinition(BoosterType.Match, coinPrice: 50), MakeDefinition(BoosterType.Shuffle, coinPrice: 40));

        Assert.AreEqual(50, _boosterManager.GetCoinPrice(BoosterType.Match));
        Assert.AreEqual(40, _boosterManager.GetCoinPrice(BoosterType.Shuffle));
    }

    [Test]
    public void GetCoinPrice_ReturnsZero_WhenNoDefinitionMatchesTheType()
    {
        SetDefinitions(MakeDefinition(BoosterType.Match, coinPrice: 50));

        Assert.AreEqual(0, _boosterManager.GetCoinPrice(BoosterType.TrayClearer));
    }

    [Test]
    public void CanUseBooster_DelegatesToTheBoosterCanExecute()
    {
        // ShuffleBooster.CanExecute() only needs a non-null BoardManager, which is always present here.
        Assert.IsTrue(_boosterManager.CanUseBooster(BoosterType.Shuffle));
    }

    [Test]
    public void UseBooster_ConsumesFreeStock_BeforeTouchingCoins()
    {
        _fakeInventory.SetCount(BoosterType.Shuffle, 1);

        bool result = _boosterManager.UseBooster(BoosterType.Shuffle);

        Assert.IsTrue(result);
        Assert.AreEqual(0, _fakeInventory.GetCount(BoosterType.Shuffle));
        Assert.AreEqual(100, _fakeCurrency.Coins, "Coins shouldn't be touched while free stock is available.");
    }

    [Test]
    public void UseBooster_FallsBackToCoins_WhenNoFreeStockLeft()
    {
        _fakeInventory.SetCount(BoosterType.Shuffle, 0);
        SetDefinitions(MakeDefinition(BoosterType.Shuffle, coinPrice: 40));

        bool result = _boosterManager.UseBooster(BoosterType.Shuffle);

        Assert.IsTrue(result);
        Assert.AreEqual(60, _fakeCurrency.Coins, "Should have charged the definition's coin price.");
    }

    [Test]
    public void UseBooster_ReturnsFalse_WhenNoFreeStockAndNotEnoughCoins()
    {
        _fakeInventory.SetCount(BoosterType.Shuffle, 0);
        _fakeCurrency.Coins = 10;
        SetDefinitions(MakeDefinition(BoosterType.Shuffle, coinPrice: 40));

        bool result = _boosterManager.UseBooster(BoosterType.Shuffle);

        Assert.IsFalse(result);
        Assert.AreEqual(10, _fakeCurrency.Coins, "A failed purchase must not charge any coins.");
    }
}
