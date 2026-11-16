/// <summary>Where the values in use come from.</summary>
public enum RemoteConfigSource
{
    /// <summary>Nothing downloaded yet on this device: only the local config assets.</summary>
    Defaults,

    /// <summary>The last values downloaded, saved on the device (offline, or the server didn't answer in time).</summary>
    Cached,

    /// <summary>Downloaded in this launch.</summary>
    Remote
}
