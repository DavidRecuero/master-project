using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoosterButtonBinding
{
    public BoosterType type;
    public BoosterButtonView view;
}

/// <summary>
/// Single owner of the booster UI. Holds the one reference to BoosterManager, subscribes
/// once to GameEvents, and pushes the resulting state to each (dumb) BoosterButtonView.
/// Also the single entry point the UI buttons should call on click.
/// </summary>
public class BoosterBarController : MonoBehaviour
{
    [SerializeField] private BoosterManager boosterManager;
    [SerializeField] private List<BoosterButtonBinding> buttons = new List<BoosterButtonBinding>();

    private void Awake()
    {
        boosterManager ??= FindFirstObjectByType<BoosterManager>();

        foreach (BoosterButtonBinding binding in buttons)
        {
            if (binding.view != null && binding.view.BoosterType != binding.type)
            {
                Debug.LogWarning($"[BoosterBarController] Binding mismatch: list says {binding.type} " +
                                  $"but the view's own BoosterType is {binding.view.BoosterType}.");
            }
        }
    }

    private void OnEnable()
    {
        GameEvents.OnTrayChanged += HandleStateChanged;
        GameEvents.OnBoardChanged += HandleStateChanged;
        GameEvents.OnBoosterUsed += HandleBoosterUsed;
    }

    private void OnDisable()
    {
        GameEvents.OnTrayChanged -= HandleStateChanged;
        GameEvents.OnBoardChanged -= HandleStateChanged;
        GameEvents.OnBoosterUsed -= HandleBoosterUsed;
    }

    private void Start()
    {
        RefreshAll();
    }

    private void HandleStateChanged() => RefreshAll();
    private void HandleBoosterUsed(BoosterType _) => RefreshAll();

    private void RefreshAll()
    {
        if (boosterManager == null) return;

        foreach (BoosterButtonBinding binding in buttons)
        {
            if (binding.view == null) continue;

            int freeCount = boosterManager.GetRemainingFreeUses(binding.type);
            int coinPrice = boosterManager.GetCoinPrice(binding.type);
            bool canExecute = boosterManager.CanUseBooster(binding.type);

            binding.view.SetState(freeCount, coinPrice, canExecute);
        }
    }

    public void UseBooster(BoosterType type)
    {
        boosterManager?.UseBooster(type);
    }

    // Unity's OnClick() Inspector only supports static parameters of type int/float/string/bool/
    // Object on a plain UnityEvent - a custom enum like BoosterType isn't supported there, so these
    // zero-arg wrappers exist purely so each button's OnClick() has something to point to.
    public void UseMatch() => UseBooster(BoosterType.Match);
    public void UseShuffle() => UseBooster(BoosterType.Shuffle);
    public void UseTrayClearer() => UseBooster(BoosterType.TrayClearer);
}