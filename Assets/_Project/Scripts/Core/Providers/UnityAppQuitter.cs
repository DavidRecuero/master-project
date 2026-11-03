using UnityEngine;

/// <summary>
/// Application.Quit on the device. In the Editor (where Application.Quit does nothing) it stops Play mode,
/// so the flow can be tested the same way.
/// </summary>
public class UnityAppQuitter : IAppQuitter
{
    public void Quit()
    {
        Debug.Log("[App] Closing the game.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
