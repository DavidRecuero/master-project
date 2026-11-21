using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Loads the songs of the music asset pack (see MusicPack) and keeps them for the whole session.
///   - Editor: reads the .ogg files from the pack folder in the project.
///   - Android: asks Play Asset Delivery for the pack (fast-follow: usually already downloaded; if not, it waits
///     for the download) and reads the files from where the device stored it.
///   - Anything else, or the pack unavailable (no Google Play, a plain APK...): no music, with a warning.
/// The songs stay compressed in memory (decoded while they play), like the old "Compressed In Memory" import.
/// </summary>
public class MusicLibrary : MonoBehaviour, IMusicLibrary
{
    private static MusicLibrary _instance;

    private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, List<Action<AudioClip>>> _waiting = new Dictionary<string, List<Action<AudioClip>>>();

    private string _packFolder;
    private bool _resolvingPack;
    private bool _packUnavailable;

    /// <summary>The shared library, created the first time it is needed. Survives scene loads.</summary>
    public static MusicLibrary Instance
    {
        get
        {
            if (_instance != null) return _instance;

            var go = new GameObject("[MusicLibrary]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MusicLibrary>();
            return _instance;
        }
    }

    public void Request(string track, Action<AudioClip> onReady)
    {
        if (string.IsNullOrEmpty(track) || onReady == null) return;

        if (_clips.TryGetValue(track, out AudioClip clip))
        {
            onReady(clip);
            return;
        }

        if (_packUnavailable) return;

        if (_waiting.TryGetValue(track, out List<Action<AudioClip>> callbacks))
        {
            callbacks.Add(onReady);
            return;
        }

        _waiting[track] = new List<Action<AudioClip>> { onReady };
        StartCoroutine(LoadTrack(track));
    }

    private IEnumerator LoadTrack(string track)
    {
        yield return ResolvePackFolder();

        if (_packFolder == null)
        {
            _waiting.Remove(track);
            yield break;
        }

        string uri = MusicPack.FileUri(MusicPack.FilePath(_packFolder, track));
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
        {
            ((DownloadHandlerAudioClip)request.downloadHandler).compressed = true;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Music] Can't load '{MusicPack.FileName(track)}': {request.error}");
                _waiting.Remove(track);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            clip.name = track;
            _clips[track] = clip;
        }

        if (_waiting.TryGetValue(track, out List<Action<AudioClip>> callbacks))
        {
            _waiting.Remove(track);
            foreach (Action<AudioClip> callback in callbacks) callback(_clips[track]);
        }
    }

    // One resolution for every track: the first request finds the folder, the rest wait for it
    private IEnumerator ResolvePackFolder()
    {
        if (_packFolder != null || _packUnavailable) yield break;

        if (_resolvingPack)
        {
            yield return new WaitUntil(() => !_resolvingPack);
            yield break;
        }

        _resolvingPack = true;
        yield return FindPackFolder(folder => _packFolder = folder);
        _resolvingPack = false;

        if (_packFolder == null)
        {
            _packUnavailable = true;
            Debug.LogWarning($"[Music] The '{MusicPack.PackName}' asset pack isn't available here. Playing without music.");
        }
        else
        {
            Debug.Log($"[Music] Music pack ready at {_packFolder}");
        }
    }

    private static IEnumerator FindPackFolder(Action<string> found)
    {
#if UNITY_EDITOR
        found(MusicPack.ProjectFolder);
        yield break;
#elif UNITY_ANDROID
        yield return FindAndroidPackFolder(found);
#else
        found(null);
        yield break;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Play Asset Delivery. The AndroidAssetPacks calls throw if the app has no Play Core (e.g. not installed
    // through Google Play as an App Bundle); a yield can't sit inside try/catch, so each call is guarded alone.
    private static IEnumerator FindAndroidPackFolder(Action<string> found)
    {
        string path = TryGetPackPath();
        if (!string.IsNullOrEmpty(path))
        {
            found(path);
            yield break;
        }

        DownloadAssetPackAsyncOperation download = null;
        try
        {
            Debug.Log($"[Music] Waiting for the '{MusicPack.PackName}' asset pack...");
            download = AndroidAssetPacks.DownloadAssetPackAsync(new[] { MusicPack.PackName });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Music] Play Asset Delivery not available: {e.Message}");
        }

        if (download == null)
        {
            found(null);
            yield break;
        }

        // Fast-follow: Google Play is usually downloading it already; this waits until it lands
        yield return download;

        path = TryGetPackPath();
        found(string.IsNullOrEmpty(path) ? null : path);
    }

    private static string TryGetPackPath()
    {
        try
        {
            return AndroidAssetPacks.GetAssetPackPath(MusicPack.PackName);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Music] Play Asset Delivery not available: {e.Message}");
            return null;
        }
    }
#endif
}
