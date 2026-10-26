using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies the right import settings when a clip is added under Assets/_Project/Audio:
/// Music/ streams as Vorbis in stereo; SFX/ is mono, decompressed on load (instant playback).
/// It only runs on import, so for files already in the project: select them and Reimport.
/// </summary>
public class AudioImportSettings : AssetPostprocessor
{
    private const string MusicFolder = "Assets/_Project/Audio/Music/";
    private const string SfxFolder = "Assets/_Project/Audio/SFX/";

    private void OnPreprocessAudio()
    {
        var importer = (AudioImporter)assetImporter;
        string path = assetPath.Replace('\\', '/');

        if (path.StartsWith(MusicFolder)) ApplyMusic(importer);
        else if (path.StartsWith(SfxFolder)) ApplySfx(importer);
    }

    private static void ApplyMusic(AudioImporter importer)
    {
        importer.forceToMono = false;
        importer.loadInBackground = true;

        AudioImporterSampleSettings s = importer.defaultSampleSettings;
        s.loadType = AudioClipLoadType.Streaming;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.7f;
        importer.defaultSampleSettings = s;
    }

    private static void ApplySfx(AudioImporter importer)
    {
        importer.forceToMono = true;
        importer.loadInBackground = false;

        AudioImporterSampleSettings s = importer.defaultSampleSettings;
        s.loadType = AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = AudioCompressionFormat.ADPCM;
        importer.defaultSampleSettings = s;
    }
}
