using UnityEngine;

/// <summary>
/// Scene glue for GameplayHapticsRouter: put it in the Level scene. It builds the router with the
/// HapticsManager and the tray, and hooks it to GameEvents while the scene is alive.
/// </summary>
public class GameplayHaptics : MonoBehaviour
{
    [SerializeField] private TrayManager trayManager;

    [Header("Tuning")]
    [Tooltip("Free tray slots at or below this give the warning vibration")]
    public int warningFreeSlots = 1;

    private IHapticsService _haptics;
    private GameplayHapticsRouter _router;

    /// <summary>Optional injection for tests or a different setup.</summary>
    public void Initialize(IHapticsService haptics)
    {
        _haptics = haptics;
    }

    private void Awake()
    {
        if (trayManager == null) trayManager = FindFirstObjectByType<TrayManager>();
        _haptics ??= HapticsManager.Instance;

        if (_haptics == null) Debug.LogWarning("[GameplayHaptics] No HapticsManager found. Play from the Boot scene to get vibrations.");
    }

    private void OnEnable()
    {
        if (_haptics == null) return;

        _router = new GameplayHapticsRouter(
            _haptics,
            trayManager != null ? () => trayManager.maxCapacity - trayManager.TrayItems.Count : (System.Func<int>)null)
        {
            warningFreeSlots = warningFreeSlots
        };
        _router.Subscribe();
    }

    private void OnDisable()
    {
        _router?.Unsubscribe();
        _router = null;
    }
}
