/// <summary>
/// Opens and closes popups as a stack: the last one opened is on top, and closing it
/// shows the one below again (Settings -> Credits -> back to Settings).
/// Menus and gameplay ask this interface for popups, so tests can swap in a fake.
/// </summary>
public interface IPopupService
{
    /// <summary>
    /// Shows a copy of the prefab on top of the others and returns it.
    /// If that prefab is already open, nothing new is created and the open copy is returned (double taps).
    /// </summary>
    T Open<T>(T prefab) where T : PopupView;

    /// <summary>Closes that popup and every popup opened on top of it.</summary>
    void Close(PopupView popup);

    /// <summary>Closes the popup on top. False if no popup was open.</summary>
    bool CloseTop();

    /// <summary>Closes every open popup.</summary>
    void CloseAll();

    /// <summary>The popup on top, or null.</summary>
    PopupView Top { get; }

    int Count { get; }
}
