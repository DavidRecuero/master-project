using System;
using System.IO;

/// <summary>
/// The music lives in an Android asset pack (Play Asset Delivery), not in the build: a custom asset pack
/// "MusicPack" with fast-follow delivery, so Google Play installs the app without it and downloads it right after.
/// Unity doesn't import files inside a ".androidpack" folder, so the songs aren't AudioClips in the project:
/// MusicLibrary loads them at runtime (from the pack on Android, straight from this folder in the Editor).
///
/// Separate from Addressables on purpose: the official Addressables + Play Asset Delivery integration turns the
/// remote catalog off, and the remote catalog is what lets new levels arrive through CCD without a new build.
/// </summary>
public static class MusicPack
{
    /// <summary>Asset pack name: the folder name without ".androidpack" and the packName in its build.gradle.</summary>
    public const string PackName = "MusicPack";

    /// <summary>Folder of the songs inside the project (the pack's src/main/assets).</summary>
    public const string ProjectFolder = "Assets/_Project/Audio/MusicPack.androidpack/src/main/assets";

    public const string Extension = ".ogg";

    /// <summary>"pleasant_creek" → "pleasant_creek.ogg".</summary>
    public static string FileName(string track) => track + Extension;

    /// <summary>Full path of a song in a folder (the pack folder on the device, or the project folder in the Editor).</summary>
    public static string FilePath(string folder, string track) => Path.Combine(folder, FileName(track));

    /// <summary>file:// URI for UnityWebRequest, with spaces and special characters escaped.</summary>
    public static string FileUri(string path) => new Uri(Path.GetFullPath(path)).AbsoluteUri;
}
