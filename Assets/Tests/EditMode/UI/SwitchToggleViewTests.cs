using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Switch look of the audio toggles: end positions, on track and the easing.</summary>
public class SwitchToggleViewTests
{
    private GameObject _root;
    private SwitchToggleView _view;
    private RectTransform _knob;
    private Image _onTrack;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Switch", typeof(RectTransform));
        _root.AddComponent<Toggle>();
        _view = _root.AddComponent<SwitchToggleView>();

        _knob = new GameObject("Knob", typeof(RectTransform)).GetComponent<RectTransform>();
        _knob.SetParent(_root.transform, false);
        _knob.anchoredPosition = new Vector2(0f, 6f);
        _onTrack = new GameObject("TrackOn", typeof(RectTransform)).AddComponent<Image>();
        _onTrack.transform.SetParent(_root.transform, false);

        PopupTestUtils.SetField(_view, "knob", _knob);
        PopupTestUtils.SetField(_view, "onTrack", _onTrack);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
    }

    [Test]
    public void Snap_On_PutsTheKnobRightAndShowsTheOnTrack()
    {
        _view.Snap(true);

        Assert.AreEqual(_view.onX, _knob.anchoredPosition.x, 1e-4f);
        Assert.AreEqual(6f, _knob.anchoredPosition.y, 1e-4f, "Only X moves");
        Assert.AreEqual(1f, _onTrack.color.a, 1e-4f);
        Assert.AreEqual(Vector3.one, _knob.localScale);
    }

    [Test]
    public void Snap_Off_PutsTheKnobLeftAndHidesTheOnTrack()
    {
        _view.Snap(false);

        Assert.AreEqual(_view.offX, _knob.anchoredPosition.x, 1e-4f);
        Assert.AreEqual(0f, _onTrack.color.a, 1e-4f);
    }

    [Test]
    public void EaseOutBack_StartsAt0_EndsAt1_AndOvershoots()
    {
        Assert.AreEqual(0f, SwitchToggleView.EaseOutBack(0f, 0.9f), 1e-4f);
        Assert.AreEqual(1f, SwitchToggleView.EaseOutBack(1f, 0.9f), 1e-4f);
        Assert.Greater(SwitchToggleView.EaseOutBack(0.8f, 0.9f), 1f);
    }

    [Test]
    public void EaseOutBack_ZeroOvershoot_NeverPassesTheEnd()
    {
        for (float t = 0f; t <= 1f; t += 0.05f)
            Assert.LessOrEqual(SwitchToggleView.EaseOutBack(t, 0f), 1f + 1e-4f);
    }
}
