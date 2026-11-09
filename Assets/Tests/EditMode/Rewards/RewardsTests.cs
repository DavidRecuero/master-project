using NUnit.Framework;
using TMPro;
using UnityEngine;

public class LevelRewardsTests
{
    [Test]
    public void EveryWin_GivesTheConfiguredCoins()
    {
        var rewards = new LevelRewards(4);

        Assert.AreEqual(4, rewards.CoinsForWin(1));
        Assert.AreEqual(4, rewards.CoinsForWin(250), "Loop levels too");
    }

    [Test]
    public void WithoutConfig_UsesTheDefault()
    {
        Assert.AreEqual(RewardsConfig.DefaultCoinsPerWin, LevelRewards.From(null).CoinsForWin(3));
    }

    [Test]
    public void FromConfig_UsesItsValue()
    {
        var config = ScriptableObject.CreateInstance<RewardsConfig>();
        config.coinsPerWin = 10;

        Assert.AreEqual(10, LevelRewards.From(config).CoinsForWin(3));
        Object.DestroyImmediate(config);
    }

    [Test]
    public void NegativeValues_GiveNothing()
    {
        Assert.AreEqual(0, new LevelRewards(-5).CoinsForWin(1));
    }
}

/// <summary>The reward row of the win popup.</summary>
public class RewardViewTests
{
    private GameObject _go;
    private RewardView _view;
    private TextMeshProUGUI _amount;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Reward", typeof(RectTransform));
        _view = _go.AddComponent<RewardView>();
        _amount = new GameObject("Amount", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        _amount.transform.SetParent(_go.transform, false);
        PopupTestUtils.SetField(_view, "amountText", _amount);
        _go.SetActive(false);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_go);

    [Test]
    public void Show_WritesTheAmountAndAppears()
    {
        _view.Show(4);

        Assert.IsTrue(_go.activeSelf);
        Assert.AreEqual("+4", _amount.text);
        Assert.AreEqual(4, _view.Coins);
    }

    [Test]
    public void Show_NothingToGive_StaysHidden()
    {
        _view.Show(0);

        Assert.IsFalse(_go.activeSelf);
    }

    [Test]
    public void Hide_HidesIt()
    {
        _view.Show(4);

        _view.Hide();

        Assert.IsFalse(_go.activeSelf);
        Assert.AreEqual(0, _view.Coins);
    }
}
