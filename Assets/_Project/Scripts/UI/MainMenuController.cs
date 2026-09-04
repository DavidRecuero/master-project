using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI playButtonText;

    private IUserDataProvider _userDataProvider;
    private ISceneLoader _sceneLoader;

    public void Initialize(IUserDataProvider userDataProvider, ISceneLoader sceneLoader)
    {
        _userDataProvider = userDataProvider;
        _sceneLoader = sceneLoader;
        UpdateUI();
    }

    private void Awake()
    {
        _userDataProvider ??= UserDataManager.Instance;
        _sceneLoader ??= new UnitySceneLoader();
    }

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        // UserDataManager validator
        if (_userDataProvider == null)
        {
            Debug.LogWarning("UserDataManager not found. Start from Boot scene.");
            return;
        }

        // Coins indicator updater
        if (coinsText != null)
            coinsText.text = _userDataProvider.Coins.ToString();

        // Current Level indicator
        if (playButtonText != null)
            playButtonText.text = $"Level {_userDataProvider.CurrentLevel}";
    }

    // Play button function
    public void OnPlayButtonClicked()
    {
        Debug.Log("Loading Gameplay...");

        //Loading "Level" scene
        _sceneLoader?.LoadScene(2);
    }
}