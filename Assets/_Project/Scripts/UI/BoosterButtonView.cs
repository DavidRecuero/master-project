using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BoosterButtonView : MonoBehaviour
{
    [SerializeField] private BoosterType boosterType;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject coinsIcon;

    [Header("Locked state (only for CanExecute() == false, never for price)")]
    [SerializeField] private float lockedAlpha = 0.4f;

    public BoosterType BoosterType => boosterType;

    private CanvasGroup _canvasGroup;
    private Button _button;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _button = GetComponent<Button>();
    }

    public void SetState(int freeCount, int coinPrice, bool canExecute)
    {
        bool hasFreeStock = freeCount > 0;

        if (priceText != null)
        {
            priceText.text = hasFreeStock ? freeCount.ToString() : coinPrice.ToString();
        }

        if (coinsIcon != null) coinsIcon.SetActive(!hasFreeStock);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = canExecute ? 1f : lockedAlpha;
            _canvasGroup.interactable = canExecute;
        }

        if (_button != null) _button.interactable = canExecute;
    }
}