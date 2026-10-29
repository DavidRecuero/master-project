using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class MainMenuController : MonoBehaviour
{
    private const string GuestIdVariable = "id";

    [Header("UI References")]
    [Tooltip("CoinsPill prefab: shows and keeps the balance in sync")]
    [SerializeField] private CoinsView coinsView;
    [Tooltip("LevelPill prefab above the Play button")]
    [SerializeField] private LevelPillView levelPill;
    [Tooltip("Name text of the ProfilePill prefab (Play Games name, or Guest)")]
    [SerializeField] private TextMeshProUGUI playerIdText;
    [Tooltip("LocalizeStringEvent on that same text. Used for the guest label; turned off while it shows the Play Games name (a name is never translated).")]
    [SerializeField] private LocalizeStringEvent playerLabel;

    [Header("Texts (UI table)")]
    [Tooltip("menu.guest: guest without an ID")]
    [SerializeField] private LocalizedString guestLabel = new LocalizedString();
    [Tooltip("menu.guest_id: guest with the start of its ID, Smart String with {id}")]
    [SerializeField] private LocalizedString guestWithIdLabel = new LocalizedString();
    [Tooltip("Photo of the ProfilePill prefab. Hidden until the Play Games picture is loaded; the default avatar shows behind it.")]
    [SerializeField] private RawImage avatarImage;
    [Tooltip("Longer Play Games names are cut with \"...\" so the pill never reaches the coins. The guest label is never cut.")]
    [SerializeField] private int maxNameLength = 14;

    [Header("Popups")]
    [Tooltip("SettingsPopup prefab, opened by the Settings button")]
    [SerializeField] private SettingsPopup settingsPopupPrefab;

    private bool _avatarLoading;

    private IUserDataProvider _userDataProvider;
    private ISceneLoader _sceneLoader;
    private IPopupService _popupService;

    public void Initialize(IUserDataProvider userDataProvider, ISceneLoader sceneLoader, IPopupService popupService = null)
    {
        _userDataProvider = userDataProvider;
        _sceneLoader = sceneLoader;
        if (popupService != null) _popupService = popupService;
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

        UpdatePlayerLabel(PlayerSession.DisplayName, _userDataProvider.UserId);

        UpdateAvatar();
    }

    /// <summary>
    /// Play Games name when signed in (cut if too long), shown as it is.
    /// Otherwise the translated guest label, with the start of the ID when there is one.
    /// </summary>
    private void UpdatePlayerLabel(string displayName, string userId)
    {
        string shownName = Truncate(displayName, maxNameLength);
        if (!string.IsNullOrEmpty(shownName))
        {
            if (playerLabel != null) playerLabel.enabled = false;   // so it doesn't write the guest label over the name
            if (playerIdText != null) playerIdText.text = shownName;
            return;
        }

        if (playerLabel == null) return;

        string shortId = ShortGuestId(userId);
        if (shortId == null)
        {
            playerLabel.StringReference = guestLabel;
        }
        else
        {
            guestWithIdLabel.SetVariable(GuestIdVariable, shortId);
            playerLabel.StringReference = guestWithIdLabel;
        }

        playerLabel.enabled = true;
    }

    /// <summary>First 8 characters of the ID (enough to tell guests apart), or null if there is no ID.</summary>
    public static string ShortGuestId(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        return userId.Length > 8 ? userId.Substring(0, 8) : userId;
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

    // Settings button function
    public void OnSettingsButtonClicked()
    {
        // Looked up on click: the PopupService comes from the Boot scene
        IPopupService popups = _popupService ?? PopupService.Instance;
        if (popups == null)
        {
            Debug.LogWarning("[MainMenu] No PopupService found. Start from Boot scene.");
            return;
        }

        popups.Open(settingsPopupPrefab);
    }
}