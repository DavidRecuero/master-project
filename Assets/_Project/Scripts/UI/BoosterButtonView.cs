using TMPro;
using UnityEngine;

public class BoosterButtonView : MonoBehaviour
{
    [SerializeField] private BoosterType boosterType;
    [SerializeField] private BoosterManager boosterManager;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject coinsIcon;

    private void Awake()
    {
        boosterManager ??= FindFirstObjectByType<BoosterManager>();
    }

    private void OnEnable()
    {
        GameEvents.OnBoosterUsed += HandleBoosterUsed;
    }

    private void OnDisable()
    {
        GameEvents.OnBoosterUsed -= HandleBoosterUsed;
    }

    private void Start()
    {
        Refresh();
    }

    private void HandleBoosterUsed(BoosterType usedType)
    {
        if (usedType == boosterType) Refresh();
    }

    public void Refresh()
    {
        if (boosterManager == null || priceText == null || coinsIcon == null) return;

        int freeCount = boosterManager.GetRemainingFreeUses(boosterType);
        bool hasFreeStock = freeCount > 0;

        priceText.text = hasFreeStock
            ? freeCount.ToString()
            : boosterManager.GetCoinPrice(boosterType).ToString();

        coinsIcon.SetActive(!hasFreeStock);
    }
}