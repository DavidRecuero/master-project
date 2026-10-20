using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Coin pill of the top HUD: shows the player's balance and keeps it in sync through GameEvents.OnCoinsChanged.
/// The whole pill is the button (the "+" is just its icon): a tap only announces the request
/// (GameEvents.OnCoinStoreRequested), the store decides what to do.
/// </summary>
public class CoinsView : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [Tooltip("Button on the whole coin pill. The \"+\" inside it is only a picture.")]
    [FormerlySerializedAs("plusButton")]
    [SerializeField] private Button storeButton;
    [Tooltip("Thousands separator: \".\" shows 1.250, \",\" shows 1,250, empty shows 1250")]
    [SerializeField] private string thousandsSeparator = ".";

    // Injected dependencies
    private IUserDataProvider _userDataProvider;

    public void Initialize(IUserDataProvider userDataProvider)
    {
        _userDataProvider = userDataProvider;
        Refresh();
    }

    private void Awake()
    {
        // Assign default dependencies if not injected before
        _userDataProvider ??= UserDataManager.Instance;
    }

    private void OnEnable()
    {
        GameEvents.OnCoinsChanged += Show;
        if (storeButton != null) storeButton.onClick.AddListener(GameEvents.TriggerCoinStoreRequested);
    }

    private void OnDisable()
    {
        GameEvents.OnCoinsChanged -= Show;
        if (storeButton != null) storeButton.onClick.RemoveListener(GameEvents.TriggerCoinStoreRequested);
    }

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        Show(_userDataProvider != null ? _userDataProvider.Coins : 0);
    }

    private void Show(int balance)
    {
        if (amountText != null) amountText.text = Format(balance, thousandsSeparator);
    }

    /// <summary>1250 -> "1.250" (with "."). Negative values are shown as 0.</summary>
    public static string Format(int amount, string separator)
    {
        string digits = Mathf.Max(0, amount).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(separator)) return digits;

        var sb = new System.Text.StringBuilder(digits.Length + digits.Length / 3);
        for (int i = 0; i < digits.Length; i++)
        {
            if (i > 0 && (digits.Length - i) % 3 == 0) sb.Append(separator);
            sb.Append(digits[i]);
        }
        return sb.ToString();
    }
}