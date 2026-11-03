using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The cloud connection for the whole session. Created by the Boot, kept in BackendServices.
///
/// ConnectAsync does what the Boot used to do on its own:
///   1. Play Games players: ask Play Games for a one-time server code. Without it, no connection
///      (never as a guest: see UgsBackendAuthService).
///   2. Start the services, sign in, sync the profile (CloudBootstrap).
/// It can be called again any time: if the game started offline, the next call (back from the
/// background, opening "Delete account") connects as soon as there is network.
///
/// While offline, every save on the device also tries to connect, with exponential backoff
/// (first try right away, then at least 30 s, 1 min, 2 min... up to 5 min between tries; back to the
/// start once connected). Saves mark the moments there is something new to upload, so no polling.
///
/// Rules:
///   - One attempt at a time: a call during an attempt waits for that same attempt.
///   - Once connected and synced, calls return right away.
///   - After the account is deleted (uploads stopped), it doesn't connect again: the Boot starts a new session.
/// </summary>
public class CloudSession : ICloudConnection
{
    private readonly IAuthProvider _platformAuth;
    private readonly IBackendAuthService _backend;
    private readonly string _environment;
    private readonly float _codeTimeoutSeconds;

    public static readonly TimeSpan FirstBackoff = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private readonly Func<DateTime> _clock;

    private Task<bool> _attempt;
    private bool _synced;
    private DateTime _nextSaveTry = DateTime.MinValue;
    private TimeSpan _backoff = FirstBackoff;

    public CloudProfileSync Sync { get; }

    public CloudSession(IAuthProvider platformAuth, IBackendAuthService backend, ICloudSaveStore store,
        IProfileRepository profiles, string environment, float codeTimeoutSeconds = 5f, Func<DateTime> clock = null)
    {
        _platformAuth = platformAuth;
        _backend = backend;
        _environment = environment;
        _codeTimeoutSeconds = codeTimeoutSeconds;
        _clock = clock ?? (() => DateTime.UtcNow);
        Sync = new CloudProfileSync(store, profiles);

        if (profiles != null) profiles.ProfileSaved += OnProfileSaved;
    }

    public bool IsConnected => _backend != null && _backend.IsSignedIn;

    /// <summary>Signed in and the profile synced (or deliberately kept, see CloudProfileSync.ProfileInUse).</summary>
    public bool IsSynced => _synced && IsConnected;

    public Task<bool> ConnectAsync()
    {
        if (Sync.IsStopped) return Task.FromResult(false);
        if (IsSynced) return Task.FromResult(true);

        // Join the attempt in progress instead of starting a second one
        if (_attempt != null && !_attempt.IsCompleted) return _attempt;

        // The task is stored BEFORE the attempt runs: the attempt itself saves the profile (taking
        // ownership), and that save calls back here; it must join this attempt, not start another one.
        var attempt = new TaskCompletionSource<bool>();
        _attempt = attempt.Task;
        RunAttempt(attempt);
        return _attempt;
    }

    // async void only here: AttemptAsync never throws, and the result goes through the TaskCompletionSource
    private async void RunAttempt(TaskCompletionSource<bool> attempt) => attempt.TrySetResult(await AttemptAsync());

    public Task FlushAsync() => Sync.HasPendingChanges ? Sync.UploadAsync() : Task.CompletedTask;

    /// <summary>When the next save may try to connect (UTC). For the tests.</summary>
    public DateTime NextSaveTry => _nextSaveTry;

    // A save while offline: try to connect, unless a recent try failed (backoff)
    private async void OnProfileSaved()
    {
        if (IsSynced || Sync.IsStopped) return;

        DateTime now = _clock();
        if (now < _nextSaveTry) return;

        _nextSaveTry = now + _backoff;   // set before trying: saves during the attempt don't start another
        bool connected = await ConnectAsync();

        if (connected)
        {
            _backoff = FirstBackoff;
            _nextSaveTry = DateTime.MinValue;
        }
        else
        {
            _backoff = TimeSpan.FromTicks(Math.Min(_backoff.Ticks * 2, MaxBackoff.Ticks));
        }
    }

    private async Task<bool> AttemptAsync()
    {
        try
        {
            string code = null;
            if (_platformAuth != null && _platformAuth.IsSignedIn && _platformAuth is IServerAuthCodeProvider codeProvider)
            {
                code = await RequestCodeAsync(codeProvider);
                if (string.IsNullOrEmpty(code))
                {
                    Debug.LogWarning("[Cloud] No Play Games server code (offline?). Staying on the data on this device for now.");
                    return false;
                }
            }

            _synced = await new CloudBootstrap(_backend, Sync, _environment).RunAsync(code);
            return _synced;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cloud] Connection attempt failed: {e.Message}");
            return false;
        }
    }

    /// <summary>The code, or null if Play Games doesn't answer in time.</summary>
    private async Task<string> RequestCodeAsync(IServerAuthCodeProvider provider)
    {
        var answer = new TaskCompletionSource<string>();
        provider.RequestServerAuthCode(code => answer.TrySetResult(code));

        if (answer.Task.IsCompleted) return answer.Task.Result;

        Task first = await Task.WhenAny(answer.Task, Task.Delay(TimeSpan.FromSeconds(_codeTimeoutSeconds)));
        return first == answer.Task ? answer.Task.Result : null;
    }
}
