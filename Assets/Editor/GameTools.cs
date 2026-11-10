using UnityEditor;
using UnityEngine;

public class GameTools
{
    [MenuItem("Tools/Reset Player Data")]
    public static void ResetData()
    {
        // To refresh UIs if used while the game is on
        if (Application.isPlaying && UserDataManager.Instance != null)
        {
            UserDataManager.Instance.ResetData();
        }
        else
        {
            // Saving a fresh profile
            var dataService = new LocalJsonUserDataService();
            dataService.SaveProfile(new UserProfile());
        }

        Debug.Log("🔧 User data reset");
    }

    /// <summary>Forgets the privacy choices, so the consent popup shows again on the next visit to the menu.</summary>
    [MenuItem("Tools/Reset Privacy Consent")]
    public static void ResetConsent()
    {
        // Through the shared service while playing, so whatever was using the data stops now
        ConsentServices.Current.Reset();
        Debug.Log("🔧 Privacy consent reset");
    }
}
