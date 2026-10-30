using UnityEngine;

/// <summary>Opens the page in the device browser.</summary>
public class UnityUrlOpener : IUrlOpener
{
    public void Open(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[UrlOpener] Empty URL, nothing to open.");
            return;
        }

        Application.OpenURL(url);
    }
}
