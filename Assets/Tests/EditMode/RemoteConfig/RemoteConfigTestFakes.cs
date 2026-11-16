using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>A Remote Config server that answers what the test says, fails, or waits until the test lets it answer.</summary>
public class FakeRemoteConfigBackend : IRemoteConfigBackend
{
    public RemoteValues Answer = RemoteValues.Empty;
    public bool Fail;
    public int Calls;

    private TaskCompletionSource<RemoteValues> _pending;

    /// <summary>The next FetchAsync waits until Release is called.</summary>
    public void HoldNextAnswer() => _pending = new TaskCompletionSource<RemoteValues>();

    public void Release(RemoteValues values) => _pending.TrySetResult(values);

    public Task<RemoteValues> FetchAsync()
    {
        Calls++;
        if (_pending != null) return _pending.Task;
        if (Fail) return Task.FromException<RemoteValues>(new InvalidOperationException("No network"));
        return Task.FromResult(Answer);
    }
}

public static class RemoteTestValues
{
    /// <summary>Values from "key", "value" pairs: Of("lives_max", "3", "reward_coins_per_win", "10").</summary>
    public static RemoteValues Of(params string[] pairs)
    {
        var values = new Dictionary<string, string>();
        for (int i = 0; i + 1 < pairs.Length; i += 2) values[pairs[i]] = pairs[i + 1];
        return new RemoteValues(values);
    }
}
