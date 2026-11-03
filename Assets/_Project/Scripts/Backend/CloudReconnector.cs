using UnityEngine;

/// <summary>
/// Keeps the cloud session alive while the game runs:
///   - Going to the background: uploads the changes a failed upload left behind.
///   - Coming back: connects if the game is offline (it started without network, or lost it).
/// Created by the Boot, survives scene loads. One per game: a new Boot replaces the session it watches.
/// </summary>
public class CloudReconnector : MonoBehaviour
{
    private static CloudReconnector _instance;

    private ICloudConnection _connection;

    /// <summary>Starts watching this session (creates the object the first time).</summary>
    public static CloudReconnector Watch(ICloudConnection connection)
    {
        if (_instance == null)
        {
            var go = new GameObject("[CloudReconnector]");
            if (Application.isPlaying) DontDestroyOnLoad(go);
            _instance = go.AddComponent<CloudReconnector>();
        }

        _instance._connection = connection;
        return _instance;
    }

    // With "Enter Play Mode Options" (no domain reload) statics survive between Play sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void OnApplicationPause(bool paused) => HandlePause(paused);

    /// <summary>What happens when the app goes to (true) or comes back from (false) the background. Public for the tests.</summary>
    public void HandlePause(bool paused)
    {
        if (_connection == null) return;

        if (paused)
        {
            _ = _connection.FlushAsync();
            return;
        }

        if (!_connection.IsConnected) Debug.Log("[Cloud] Back in the game while offline: trying to connect.");
        _ = _connection.ConnectAsync();
    }
}
