using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// Lives pill of the main menu top bar: a heart with the number of lives and, next to it, "Full" or the
/// time left for the next life. The whole pill is a button that opens the lives popup.
/// The countdown is digits only, so it is written straight to the text; "Full" is a translated string
/// (the LocalizeStringEvent is switched off while the countdown is shown, so it doesn't write over it).
/// It reads the lives a few times per second: they come back with time, there is no event for that.
/// </summary>
public class LivesView : MonoBehaviour
{
    [Header("Heart")]
    [SerializeField] private Image heartImage;
    [SerializeField] private Sprite fullHeart;
    [Tooltip("Shown with no lives left")]
    [SerializeField] private Sprite emptyHeart;
    [Tooltip("Number of lives, drawn on the heart")]
    [SerializeField] private TMP_Text countText;

    [Header("Status")]
    [Tooltip("\"Full\" or the countdown")]
    [SerializeField] private TMP_Text statusText;
    [Tooltip("LocalizeStringEvent on that same text. Leave its String Reference empty: the pill sets it.")]
    [SerializeField] private LocalizeStringEvent statusLabel;
    [Tooltip("UI/lives.full_short")]
    [SerializeField] private LocalizedString fullText = new LocalizedString();

    [Header("Popup")]
    [Tooltip("Button on the whole pill")]
    [SerializeField] private Button button;
    [Tooltip("LivesPopup prefab, opened when the pill is tapped")]
    [SerializeField] private LivesPopup livesPopupPrefab;

    [Tooltip("Seconds between reads of the lives (the countdown moves once per second)")]
    [SerializeField] private float refreshSeconds = 0.25f;

    private ILivesService _lives;
    private IPopupService _popups;
    private float _nextRefresh;
    private int _shownLives = -1;
    private string _shownTime;

    public void Initialize(ILivesService lives, IPopupService popups = null)
    {
        Unsubscribe();
        _lives = lives;
        if (popups != null) _popups = popups;
        _shownLives = -1;
        _shownTime = null;
        Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        if (_lives == null && UserDataManager.Instance != null) _lives = UserDataManager.Instance.Lives;
        Subscribe();
        if (button != null) button.onClick.AddListener(OnClicked);
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (button != null) button.onClick.RemoveListener(OnClicked);
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + refreshSeconds;
        Refresh();
    }

    /// <summary>Shows the current lives. Public so it can be tested without waiting for Update.</summary>
    public void Refresh()
    {
        if (_lives == null) return;

        // Only what changed is written (it runs a few times per second)
        int lives = _lives.Lives;
        if (lives != _shownLives)
        {
            _shownLives = lives;
            if (countText != null) countText.text = lives.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (heartImage != null)
            {
                Sprite sprite = lives > 0 ? fullHeart : emptyHeart;
                if (sprite != null) heartImage.sprite = sprite;
            }
        }

        if (_lives.IsFull)
        {
            if (statusLabel == null) return;
            if (!ReferenceEquals(statusLabel.StringReference, fullText)) statusLabel.StringReference = fullText;
            statusLabel.enabled = true;
            _shownTime = null;
            return;
        }

        if (statusLabel != null) statusLabel.enabled = false;
        string time = LivesFormat.Countdown(_lives.TimeToNextLife);
        if (time == _shownTime) return;
        _shownTime = time;
        if (statusText != null) statusText.text = time;
    }

    /// <summary>Opens the lives popup. Looked up on click: the PopupService comes from the Boot scene.</summary>
    public void OnClicked()
    {
        IPopupService popups = _popups ?? PopupService.Instance;
        if (popups == null || livesPopupPrefab == null)
        {
            Debug.LogWarning("[LivesView] No PopupService or LivesPopup prefab. Start from the Boot scene.");
            return;
        }

        LivesPopup popup = popups.Open(livesPopupPrefab);
        if (popup != null && _lives != null) popup.Initialize(_lives);
    }

    private void Subscribe()
    {
        if (_lives != null)
        {
            _lives.LivesChanged -= Refresh;
            _lives.LivesChanged += Refresh;
        }
    }

    private void Unsubscribe()
    {
        if (_lives != null) _lives.LivesChanged -= Refresh;
    }
}
