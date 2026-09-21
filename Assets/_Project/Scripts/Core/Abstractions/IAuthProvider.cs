using System;

// Interface to abstract player authentication
public interface IAuthProvider
{
    bool IsSignedIn { get; }
    string PlayerId { get; }
    string DisplayName { get; }

    void SignIn(Action<AuthResult> onComplete);
}