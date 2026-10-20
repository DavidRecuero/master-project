using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class CoinsViewTests
{
    private GameObject _gameObject;
    private CoinsView _coinsView;
    private TextMeshProUGUI _amountText;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("CoinsView");
        _coinsView = _gameObject.AddComponent<CoinsView>();
        _amountText = new GameObject("Amount").AddComponent<TextMeshProUGUI>();
        _amountText.transform.SetParent(_gameObject.transform);

        typeof(CoinsView)
            .GetField("amountText", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_coinsView, _amountText);

        typeof(CoinsView)
            .GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_coinsView, null);
    }

    [TearDown]
    public void TearDown()
    {
        if (_coinsView != null)
        {
            typeof(CoinsView)
                .GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(_coinsView, null);
        }

        Object.DestroyImmediate(_gameObject);
    }

    [Test]
    public void Initialize_ShowsTheInjectedBalance()
    {
        _coinsView.Initialize(new FakeUserDataProvider { Coins = 1250 });

        Assert.AreEqual("1.250", _amountText.text);
    }

    [Test]
    public void CoinsChangedEvent_UpdatesTheAmount()
    {
        _coinsView.Initialize(new FakeUserDataProvider { Coins = 300 });

        GameEvents.TriggerCoinsChanged(250);

        Assert.AreEqual("250", _amountText.text);
    }
}
