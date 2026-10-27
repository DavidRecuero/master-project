using UnityEngine;

/// <summary>
/// Root component of every popup prefab. The PopupService creates the popup, tells it when it
/// is opened and closed, and destroys it on close. Hook the close (X) button, and the dim if
/// tapping outside should close it, to Close().
/// Can be used as is for simple popups, or extended (SettingsPopup, CreditsPopup).
/// </summary>
public class PopupView : MonoBehaviour
{
    /// <summary>The service that opened this popup (null if it was placed by hand in a scene).</summary>
    public IPopupService Service { get; private set; }

    /// <summary>The prefab this popup was created from. The service uses it to avoid opening the same popup twice.</summary>
    public PopupView SourcePrefab { get; private set; }

    internal void Bind(IPopupService service, PopupView sourcePrefab)
    {
        Service = service;
        SourcePrefab = sourcePrefab;
    }

    internal void NotifyOpened() => OnOpened();
    internal void NotifyClosed() => OnClosed();

    /// <summary>Called once the popup is created and in the stack: read the data to show here.</summary>
    protected virtual void OnOpened() { }

    /// <summary>Called just before the popup is destroyed.</summary>
    protected virtual void OnClosed() { }

    /// <summary>For the close button (and the dim). Closes this popup and any popup opened on top of it.</summary>
    public void Close()
    {
        if (Service != null)
        {
            Service.Close(this);
            return;
        }

        // Not opened through the service (e.g. placed in a scene to test it): just remove it
        PopupService.DestroyObject(gameObject);
    }
}
