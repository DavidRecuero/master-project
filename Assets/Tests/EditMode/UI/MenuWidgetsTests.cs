using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

/// <summary>Shared menu widgets: LevelPill text and the PrimaryButton animation curves.</summary>
public class MenuWidgetsTests
{
    [Test]
    public void LevelPill_ShowsTheLevel_AndNeverBelowOne()
    {
        var go = new GameObject("LevelPill");
        var label = new GameObject("Label").AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(go.transform);
        var pill = go.AddComponent<LevelPillView>();
        typeof(LevelPillView).GetField("label", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(pill, label);

        pill.SetLevel(12);
        Assert.AreEqual("<size=62%>LEVEL</size> 12", label.text);
        Assert.AreEqual(12, pill.Level);

        Assert.AreEqual("LEVEL 1", LevelPillView.Format("LEVEL {0}", 0));
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
