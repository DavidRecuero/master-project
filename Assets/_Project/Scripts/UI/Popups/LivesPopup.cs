using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// Lives popup, opened from the lives pill of the main menu.
///   Hearts  - one per life of the maximum: full (red) for the lives the player has, empty (muted blue) for the missing ones.
///             The prefab has one heart; the rest are copies of it, so a different maximum needs no prefab change.
///   Status  - "You have all your lives!" or "Next life in 12:34".
///   Refill  - only when lives are missing: fills them all for (coins per life x missing lives).
///             Disabled, with a "not enough coins" line, when the player can't pay.
/// It reads the lives a few times per second, so the countdown moves and a life that comes back shows up.
/// </summary>
public class LivesPopup : PopupView
{
    public const string TimeVariable = "time";
    public const string PriceVariable = "price";

    [Header("Hearts")]
    [Tooltip("The heart in the prefab. It is copied next to itself until there is one per life of the maximum.")]
    [SerializeField] private Image heartTemplate;
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    [Header("Status")]
    [Tooltip("LocalizeStringEvent of the status line (String Reference empty: the popup sets it)")]
    [SerializeField] private LocalizeStringEvent statusLabel;
    [Tooltip("UI/lives.full")]
    [SerializeField] private LocalizedString fullText = new LocalizedString();
    [Tooltip("UI/lives.next_life, Smart String with {time}")]
    [SerializeField] private LocalizedString nextLifeText = new LocalizedString();

    [Header("Refill")]
    [SerializeField] private Button refillButton;
    [Tooltip("LocalizeStringEvent of the button text (String Reference empty)")]
    [SerializeField] private LocalizeStringEvent refillLabel;
    [Tooltip("UI/lives.refill, Smart String with {price}")]
    [SerializeField] private LocalizedString refillText = new LocalizedString();
    [Tooltip("\"Not enough coins\" line, shown when the refill can't be paid")]
    [SerializeField] private GameObject notEnoughCoins;

    [Tooltip("Seconds between reads of the lives")]
    [SerializeField] private float refreshSeconds = 0.25f;

    private readonly List<Image> _hearts = new List<Image>();
    private ILivesService _lives;
    private float _nextRefresh;
    private string _shownTime;
    private int _shownPrice = -1;

    /// <summary>Hearts on screen (one per life of the maximum).</summary>
    public IReadOnlyList<Image> Hearts => _hearts;

    /// <summary>Optional injection (tests). Without it, the lives of the UserDataManager are used.</summary>
    public void Initialize(ILivesService lives)
    {
        Unsubscribe();
        _lives = lives;
        Subscribe();
        Refresh();
    }

    private void Awake()
    {
        if (refillButton != null) refillButton.onClick.AddListener(OnRefillClicked);
    }

    private void OnDestroy()
    {
        if (refillButton != null) refillButton.onClick.RemoveListener(OnRefillClicked);
        Unsubscribe();
    }

    protected override void OnOpened()
    {
        if (_lives == null && UserDataManager.Instance != null) _lives = UserDataManager.Instance.Lives;
        Subscribe();
        Refresh();
    }

    protected override void OnClosed() => Unsubscribe();

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

        int lives = _lives.Lives;
        BuildHearts(_lives.MaxLives);
        for (int i = 0; i < _hearts.Count; i++)
        {
            Sprite sprite = i < lives ? fullHeart : emptyHeart;
            if (sprite != null) _hearts[i].sprite = sprite;
        }

        bool full = _lives.IsFull;

        if (full)
        {
            Show(statusLabel, fullText);
        }
        else
        {
            string time = LivesFormat.Countdown(_lives.TimeToNextLife);
            if (time != _shownTime)
            {
                nextLifeText.SetVariable(TimeVariable, time);
                _shownTime = time;
            }
            Show(statusLabel, nextLifeText);
        }

        int price = full ? 0 : _lives.RefillPrice;
        bool canPay = !full && _lives.CanAfford(price);

        if (refillButton != null)
        {
            refillButton.gameObject.SetActive(!full);
            refillButton.interactable = canPay;
        }

        if (!full)
        {
            if (price != _shownPrice)
            {
                refillText.SetVariable(PriceVariable, price);
                _shownPrice = price;
            }
            Show(refillLabel, refillText);
        }

        if (notEnoughCoins != null) notEnoughCoins.SetActive(!full && !canPay);
    }

    /// <summary>Refill button: buys every missing life.</summary>
    public void OnRefillClicked()
    {
        if (_lives == null) return;

        _lives.TryRefill();
        Refresh();
    }

    // One heart per life of the maximum: the template plus copies of it. Extra hearts (smaller maximum) are hidden.
    private void BuildHearts(int count)
    {
        if (heartTemplate == null) return;
        if (_hearts.Count == 0) _hearts.Add(heartTemplate);

        while (_hearts.Count < count)
        {
            Image copy = Instantiate(heartTemplate, heartTemplate.transform.parent, false);
            copy.name = $"{heartTemplate.name} ({_hearts.Count + 1})";
            _hearts.Add(copy);
        }

        for (int i = 0; i < _hearts.Count; i++) _hearts[i].gameObject.SetActive(i < count);
        if (_hearts.Count > count) _hearts.RemoveRange(count, _hearts.Count - count);
    }

    private static void Show(LocalizeStringEvent label, LocalizedString text)
    {
        if (label != null && !ReferenceEquals(label.StringReference, text)) label.StringReference = text;
    }

    private void Subscribe()
    {
        if (_lives == null) return;
        _lives.LivesChanged -= Refresh;
        _lives.LivesChanged += Refresh;
    }

    private void Unsubscribe()
    {
        if (_lives != null) _lives.LivesChanged -= Refresh;
    }
}
