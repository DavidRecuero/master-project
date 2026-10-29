using System;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// A TextAsset that changes with the language (one file per language in an Asset Table).
/// The package ships LocalizedSprite, LocalizedAudioClip... but not one for TextAsset, so this is it.
/// Used by the credits (Data/Credits/Credits_en.txt, Credits_es.txt, Credits_ca.txt).
/// </summary>
[Serializable]
public class LocalizedTextAsset : LocalizedAsset<TextAsset> { }
