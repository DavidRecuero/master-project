using System;
using UnityEngine;

/// <summary>Gives the songs by name, as soon as they are available (they may still be downloading after install).</summary>
public interface IMusicLibrary
{
    /// <summary>
    /// Calls onReady with the song when it is loaded: right away if it already is, later if the music pack is
    /// still downloading, never if the music can't be had (the game just plays without music).
    /// </summary>
    void Request(string track, Action<AudioClip> onReady);
}
