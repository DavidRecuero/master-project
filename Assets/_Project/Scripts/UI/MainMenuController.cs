using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI playButtonText;

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        // UserDataManager validator
        if (UserDataManager.Instance == null)
        {
            Debug.LogWarning("UserDataManager not found. Start from Boot scene.");
            return;
        }

        // Coins indicator updater
        int currentCoins = UserDataManager.Instance.Profile.Coins;
        coinsText.text = currentCoins.ToString();

        // Current Level indicator
        int currentLevel = UserDataManager.Instance.Profile.CurrentLevel;
        playButtonText.text = $"Level {currentLevel}";
    }

    // Play button function
    public void OnPlayButtonClicked()
    {
        Debug.Log("Loading Gameplay...");
        
        //Loading "Level" scene
        SceneManager.LoadScene(2);
    }
}