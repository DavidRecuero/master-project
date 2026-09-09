using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BoosterButtonView : MonoBehaviour
{
    [SerializeField] private BoosterType boosterType;
    [SerializeField] private BoosterManager boosterManager;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject coinsIcon;

    [Header("Locked state")]
    [SerializeField] private float lockedAlpha = 0.4f;
    [SerializeField] private float lockCheckInterval = 0.25f;

    private CanvasGroup _canvasGroup;
    private Button _button;
    private float _lockCheckTimer;

    private void Awake()
    {
        boosterManager ??= FindFirstObjectByType<BoosterManager>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _button = GetComponent<Button>();
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
        RefreshLockState();
    }

    private void Update()
    {
        _lockCheckTimer += Time.deltaTime;
        if (_lockCheckTimer >= lockCheckInterval)
        {
            _lockCheckTimer = 0f;
            RefreshLockState();
        }
    }

    private void HandleBoosterUsed(BoosterType usedType)
    {
        if (usedType == boosterType) Refresh();
        RefreshLockState();
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

    private void RefreshLockState()
    {
        if (boosterManager == null || _canvasGroup == null) return;

        bool canExecute = boosterManager.CanUseBooster(boosterType);

        _canvasGroup.alpha = canExecute ? 1f : lockedAlpha;
        _canvasGroup.interactable = canExecute;

        if (_button != null) _button.interactable = canExecute;
    }
}