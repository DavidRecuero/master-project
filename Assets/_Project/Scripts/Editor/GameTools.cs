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
}