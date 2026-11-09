using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Reward row of the win popup: coin icon + "+4". Digits only, so nothing to translate.
/// UIManager shows it on a win with the coins the player got, and hides it otherwise.
/// </summary>
public class RewardView : MonoBehaviour
{
    [Tooltip("Amount text next to the coin")]
    [SerializeField] private TMP_Text amountText;

    public int Coins { get; private set; }

    /// <summary>Shows "+coins". Hidden when there is nothing to give.</summary>
    public void Show(int coins)
    {
        Coins = Mathf.Max(0, coins);
        if (amountText != null) amountText.text = Format(Coins);
        gameObject.SetActive(Coins > 0);
    }

    public void Hide()
    {
        Coins = 0;
        gameObject.SetActive(false);
    }

    public static string Format(int coins) => "+" + Mathf.Max(0, coins).ToString(CultureInfo.InvariantCulture);
}
