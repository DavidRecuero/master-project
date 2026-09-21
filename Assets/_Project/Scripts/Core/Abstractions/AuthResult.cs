public enum AuthResult
{
    Success,
    Cancelled,  // the player closed the Google sign-in dialog
    NoNetwork,
    Error       // a real SDK/server failure
}