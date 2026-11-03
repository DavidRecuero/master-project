using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Cloud Save in memory. Completes right away (or fails) unless a test holds the requests
/// (HoldSaves) to check what happens while one is in flight.
/// </summary>
public class FakeCloudSaveStore : ICloudSaveStore
{
    public readonly Dictionary<string, string> Data = new Dictionary<string, string>();
    public int SaveCount { get; private set; }
    public bool FailLoads { get; set; }
    public bool FailSaves { get; set; }
    public bool FailDeletes { get; set; }
    public int DeleteCount { get; private set; }
    public bool HoldSaves { get; set; }

    private readonly Queue<TaskCompletionSource<bool>> _heldSaves = new Queue<TaskCompletionSource<bool>>();

    public int HeldSaves => _heldSaves.Count;

    public Task<string> LoadAsync(string key)
    {
        if (FailLoads) return Task.FromException<string>(new Exception("load failed"));
        return Task.FromResult(Data.TryGetValue(key, out string value) ? value : null);
    }

    public Task SaveAsync(string key, string value)
    {
        if (FailSaves) return Task.FromException(new Exception("save failed"));

        SaveCount++;
        Data[key] = value;

        if (!HoldSaves) return Task.CompletedTask;

        var held = new TaskCompletionSource<bool>();
        _heldSaves.Enqueue(held);
        return held.Task;
    }

    public Task DeleteAsync(string key)
    {
        if (FailDeletes) return Task.FromException(new Exception("delete failed"));

        DeleteCount++;
        Data.Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>Finishes the oldest held save.</summary>
    public void ReleaseOneSave() => _heldSaves.Dequeue().SetResult(true);
}

/// <summary>The device profile in memory, counting the saves.</summary>
public class FakeProfileRepository : IProfileRepository
{
    public UserProfile Profile { get; set; } = new UserProfile();
    public event Action ProfileSaved;
    public int ReplaceCount { get; private set; }

    public void ReplaceProfile(UserProfile profile)
    {
        ReplaceCount++;
        Profile = profile;
        ProfileSaved?.Invoke();
    }

    public void SetOwner(string playerId)
    {
        Profile.UserId = playerId;
        ProfileSaved?.Invoke();
    }

    /// <summary>What the game does on a level cleared, a booster used...</summary>
    public void SimulateGameSave(Action<UserProfile> change)
    {
        change(Profile);
        ProfileSaved?.Invoke();
    }
}

public class FakeBackendAuthService : IBackendAuthService
{
    public bool IsSignedIn { get; private set; }
    /// <summary>The account the fake signs in to.</summary>
    public string AccountId { get; set; } = "ugs-player";
    public string Environment { get; private set; }
    public string CodeUsed { get; private set; }
    public bool SignInCalled { get; private set; }
    public bool FailInitialize { get; set; }
    public bool FailSignIn { get; set; }
    public bool FailDelete { get; set; }
    public bool AccountDeleted { get; private set; }

    public string PlayerId => IsSignedIn ? AccountId : "";

    public Task InitializeAsync(string environment)
    {
        Environment = environment;
        return FailInitialize ? Task.FromException(new Exception("init failed")) : Task.CompletedTask;
    }

    public Task SignInAsync(string playGamesAuthCode)
    {
        SignInCalled = true;
        CodeUsed = playGamesAuthCode;
        if (FailSignIn) return Task.FromException(new Exception("sign-in failed"));

        IsSignedIn = true;
        return Task.CompletedTask;
    }

    public Task DeleteAccountAsync()
    {
        if (FailDelete) return Task.FromException(new Exception("delete failed"));

        AccountDeleted = true;
        IsSignedIn = false;
        return Task.CompletedTask;
    }

    /// <summary>Signs in without going through InitializeAsync/SignInAsync.</summary>
    public void SignInNow() => IsSignedIn = true;
}
