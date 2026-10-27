using System.Collections.Generic;
using UnityEngine;

/// <summary>Records which popups the code under test asked to open. Creates nothing.</summary>
public class FakePopupService : IPopupService
{
    public readonly List<PopupView> Opened = new List<PopupView>();
    public int CloseAllCalls { get; private set; }

    public T Open<T>(T prefab) where T : PopupView
    {
        Opened.Add(prefab);
        return prefab;
    }

    public void Close(PopupView popup) => Opened.Remove(popup);

    public bool CloseTop()
    {
        if (Opened.Count == 0) return false;
        Opened.RemoveAt(Opened.Count - 1);
        return true;
    }

    public void CloseAll()
    {
        CloseAllCalls++;
        Opened.Clear();
    }

    public PopupView Top => Opened.Count > 0 ? Opened[Opened.Count - 1] : null;
    public int Count => Opened.Count;
}

/// <summary>A popup that counts its open/close notifications.</summary>
public class CountingPopup : PopupView
{
    public int OpenedCalls;
    public int ClosedCalls;

    protected override void OnOpened() => OpenedCalls++;
    protected override void OnClosed() => ClosedCalls++;
}

public static class PopupTestUtils
{
    /// <summary>Sets a private [SerializeField] the way the Inspector would.</summary>
    public static void SetField(object target, string field, object value)
    {
        var info = target.GetType().GetField(field,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert(info != null, $"Field '{field}' not found on {target.GetType().Name}");
        info.SetValue(target, value);
    }

    /// <summary>A "prefab" for the tests: a UI object with the popup component, kept out of the stack.</summary>
    public static T MakePrefab<T>(string name) where T : PopupView
    {
        var go = new GameObject(name, typeof(RectTransform));
        return go.AddComponent<T>();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}
