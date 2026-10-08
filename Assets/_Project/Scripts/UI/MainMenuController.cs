using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI playButtonText;
    [SerializeField] private TextMeshProUGUI playerIdText;
    [SerializeField] private RawImage avatarImage;
    private bool _avatarLoading;

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

        // Player identity indicator (guest id today, real Google name later)
        if (playerIdText != null)
            playerIdText.text = FormatPlayerLabel(PlayerSession.DisplayName, _userDataProvider.UserId);

        UpdateAvatar();
    }

    public static string FormatPlayerLabel(string displayName, string userId)
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;
        if (string.IsNullOrEmpty(userId)) return "Guest";

        string shortId = userId.Length > 8 ? userId.Substring(0, 8) : userId;
        return $"Guest: {shortId}";
    }

    private void UpdateAvatar()
    {
        if (avatarImage == null) return;

        if (PlayerSession.Avatar != null)
        {
            ShowAvatar(PlayerSession.Avatar);
            return;
        }

        avatarImage.gameObject.SetActive(false);
        if (!_avatarLoading && !string.IsNullOrEmpty(PlayerSession.AvatarUrl))
            StartCoroutine(LoadAvatar(PlayerSession.AvatarUrl));
    }

    private IEnumerator LoadAvatar(string url)
    {
        _avatarLoading = true;

        using (var request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                PlayerSession.Avatar = DownloadHandlerTexture.GetContent(request);
            else
                Debug.LogWarning($"[MainMenu] Avatar download failed: {request.error}");
        }

        _avatarLoading = false;
        if (PlayerSession.Avatar != null) ShowAvatar(PlayerSession.Avatar);
    }

    private void ShowAvatar(Texture texture)
    {
        if (avatarImage == null) return;   // object destroyed while downloading
        avatarImage.texture = texture;
        avatarImage.gameObject.SetActive(true);
    }

    // Play button function
    public void OnPlayButtonClicked()
    {
        Debug.Log("Loading Gameplay...");

        //Loading "Level" scene
        _sceneLoader?.LoadScene(2);
    }
}