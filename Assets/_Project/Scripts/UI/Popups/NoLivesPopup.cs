using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// "Out of lives" popup. LivesGate opens it instead of the level when the player taps Play (main menu)
/// or Retry (result screen) with no lives left.
///   No lives    - time to the next life, and a button to buy one life with coins. Buying it starts the level.
///                 Disabled, with a "not enough coins" line, when the player can't pay.
///   Life ready  - a life came back while the popup was open: the button turns into a free PLAY.
/// The X, the dim and the back button close it and leave the player where they were.
/// </summary>
public class NoLivesPopup : PopupView
{
    public const string TimeVariable = "time";
    public const string PriceVariable = "price";

    [Header("Button")]
    [Tooltip("Buys one life and plays, or just plays once a life is back")]
    [SerializeField] private Button actionButton;
    [Tooltip("LocalizeStringEvent of the button text (String Reference empty)")]
    [SerializeField] private LocalizeStringEvent actionLabel;
    [Tooltip("UI/nolives.buy, Smart String with {price}")]
    [SerializeField] private LocalizedString buyText = new LocalizedString();
    [Tooltip("UI/menu.play")]
    [SerializeField] private LocalizedString playText = new LocalizedString();
    [Tooltip("Coin next to the price, hidden when the button is a free PLAY")]
    [SerializeField] private GameObject coinIcon;
    [Tooltip("\"Not enough coins\" line")]
    [SerializeField] private GameObject notEnoughCoins;

    [Header("Status")]
    [Tooltip("LocalizeStringEvent of the line with the countdown (String Reference empty)")]
    [SerializeField] private LocalizeStringEvent statusLabel;
    [Tooltip("UI/lives.next_life, Smart String with {time}")]
    [SerializeField] private LocalizedString nextLifeText = new LocalizedString();
    [Tooltip("UI/lives.ready")]
    [SerializeField] private LocalizedString readyText = new LocalizedString();

    [Tooltip("Seconds between reads of the lives")]
    [SerializeField] private float refreshSeconds = 0.25f;

    private ILivesService _lives;
    private Action _onPlay;
    private float _nextRefresh;
    private string _shownTime;
    private int _shownPrice = -1;

    /// <summary>True when a life is there and the button just plays.</summary>
    public bool LifeReady => _lives != null && _lives.HasLives;

    /// <param name="onPlay">Starts the level (LivesGate passes it). Called once, after the popup closes.</param>
    public void Initialize(ILivesService lives, Action onPlay)
    {
        Unsubscribe();
        _lives = lives;
        _onPlay = onPlay;
        Subscribe();
        Refresh();
    }

    private void Awake()
    {
        if (actionButton != null) actionButton.onClick.AddListener(OnActionClicked);
    }

    private void OnDestroy()
    {
        if (actionButton != null) actionButton.onClick.RemoveListener(OnActionClicked);
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

    /// <summary>Shows the current state. Public so it can be tested without waiting for Update.</summary>
    public void Refresh()
    {
        if (_lives == null) return;

        if (_lives.HasLives)
        {
            Show(actionLabel, playText);
            Show(statusLabel, readyText);
            if (coinIcon != null) coinIcon.SetActive(false);
            if (notEnoughCoins != null) notEnoughCoins.SetActive(false);
            if (actionButton != null) actionButton.interactable = true;
            return;
        }

        string time = LivesFormat.Countdown(_lives.TimeToNextLife);
        if (time != _shownTime)
        {
            nextLifeText.SetVariable(TimeVariable, time);
            _shownTime = time;
        }
        Show(statusLabel, nextLifeText);

        int price = _lives.LifePrice;
        if (price != _shownPrice)
        {
            buyText.SetVariable(PriceVariable, price);
            _shownPrice = price;
        }
        Show(actionLabel, buyText);

        bool canPay = _lives.CanAfford(price);
        if (coinIcon != null) coinIcon.SetActive(true);
        if (notEnoughCoins != null) notEnoughCoins.SetActive(!canPay);
        if (actionButton != null) actionButton.interactable = canPay;
    }

    /// <summary>Buys a life (if needed) and starts the level.</summary>
    public void OnActionClicked()
    {
        if (_lives == null) return;

        if (_lives.HasLives || _lives.TryBuyLife())
        {
            Play();
            return;
        }

        Refresh();   // couldn't pay
    }

    private void Play()
    {
        Action play = _onPlay;
        _onPlay = null;   // once: a double tap doesn't load the level twice

        Close();
        play?.Invoke();
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
