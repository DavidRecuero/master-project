using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

/// <summary>Shared menu widgets: LevelPill level and the PrimaryButton animation curves.</summary>
public class MenuWidgetsTests
{
    [Test]
    public void LevelPill_ShowsTheLevel_AndNeverBelowOne()
    {
        var go = new GameObject("LevelPill");
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform);
        labelGo.AddComponent<TextMeshProUGUI>();
        var label = labelGo.AddComponent<LocalizeStringEvent>();
        var pill = go.AddComponent<LevelPillView>();
        typeof(LevelPillView).GetField("label", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(pill, label);

        pill.SetLevel(12);
        Assert.AreEqual(12, pill.Level);
        Assert.AreEqual(12, label.StringReference.GetVariableValue(LevelPillView.LevelVariable), "{level} of common.level");

        pill.SetLevel(0);
        Assert.AreEqual(1, pill.Level);
        Assert.AreEqual(1, label.StringReference.GetVariableValue(LevelPillView.LevelVariable));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void ButtonLetters_RestMostOfTheLoop_AndHopOnce()
    {
        Assert.AreEqual(new Vector3(1f, 1f, 0f), PrimaryButtonFx.HopPose(0.2f));
        Assert.AreEqual(new Vector3(1f, 1f, 0f), PrimaryButtonFx.HopPose(0.9f));

        Vector3 top = PrimaryButtonFx.HopPose(0.57f);
        Assert.AreEqual(1f, top.z, 1e-4f, "Highest point");
        Assert.Greater(top.y, 1f, "Stretched while in the air");

        Vector3 landing = PrimaryButtonFx.HopPose(0.64f);
        Assert.AreEqual(0f, landing.z, 1e-4f);
        Assert.Less(landing.y, 1f, "Squashed on landing");
    }

    [Test]
    public void ButtonShine_CrossesAfterTheLetters()
    {
        Assert.AreEqual(0f, PrimaryButtonFx.ShineProgress(0.5f));
        Assert.AreEqual(1f, PrimaryButtonFx.ShineProgress(0.9f));
        Assert.That(PrimaryButtonFx.ShineProgress(0.72f), Is.InRange(0.1f, 0.9f));
    }
}
