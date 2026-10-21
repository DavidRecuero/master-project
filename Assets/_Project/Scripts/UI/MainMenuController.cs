using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("CoinsPill prefab: shows and keeps the balance in sync")]
    [SerializeField] private CoinsView coinsView;
    [Tooltip("LevelPill prefab above the Play button")]
    [SerializeField] private LevelPillView levelPill;
    [Tooltip("Name text of the ProfilePill prefab (Play Games name, or Guest)")]
    [SerializeField] private TextMeshProUGUI playerIdText;
    [Tooltip("Photo of the ProfilePill prefab. Hidden until the Play Games picture is loaded; the default avatar shows behind it.")]
    [SerializeField] private RawImage avatarImage;
    [Tooltip("Longer names are cut with an ellipsis so the pill never reaches the coins")]
    [SerializeField] private int maxNameLength = 14;
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

        // Coins indicator: same view (and same injection) as in the gameplay HUD
        if (coinsView != null) coinsView.Initialize(_userDataProvider);

        // Current level, on the shared LevelPill
        if (levelPill != null) levelPill.SetLevel(_userDataProvider.CurrentLevel);

        // Player identity: Play Games name when signed in, guest label otherwise
        if (playerIdText != null)
            playerIdText.text = Truncate(FormatPlayerLabel(PlayerSession.DisplayName, _userDataProvider.UserId), maxNameLength);

        UpdateAvatar();
    }

    public static string FormatPlayerLabel(string displayName, string userId)
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;
        if (string.IsNullOrEmpty(userId)) return "Guest";

        string shortId = userId.Length > 8 ? userId.Substring(0, 8) : userId;
        return $"Guest: {shortId}";
    }

    /// <summary>Cuts text longer than maxLength and ends it with "..." (plain dots: the static fonts only hold Latin-1).</summary>
    public static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || maxLength <= 0 || text.Length <= maxLength) return text;
        return text.Substring(0, Mathf.Max(1, maxLength - 3)).TrimEnd() + "...";
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