using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Opens popups from anywhere (menu, gameplay...) on its own canvas, above every scene's UI.
/// Lives in the Boot scene and survives scene loads, like the AudioManager.
/// Popups are a stack: each new one goes on top, the Android back button closes the top one,
/// and loading another scene closes them all.
/// </summary>
public class PopupService : MonoBehaviour, IPopupService
{
    public static PopupService Instance { get; private set; }

    [Header("Canvas")]
    [Tooltip("Above the scenes' canvases (they use 0)")]
    [SerializeField] private int sortingOrder = 100;
    [Tooltip("Same as the scenes' Canvas Scaler, so popups have the same size as the rest of the UI")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1170, 2532);
    [Range(0f, 1f)]
    [SerializeField] private float matchWidthOrHeight = 0f;

    [Header("Behaviour")]
    [Tooltip("The Android back button (Escape in the editor) closes the popup on top")]
    [SerializeField] private bool closeTopOnBack = true;

    private readonly List<PopupView> _stack = new List<PopupView>();
    private RectTransform _root;

    public PopupView Top
    {
        get
        {
            Prune();
            return _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        }
    }

    public int Count
    {
        get
        {
            Prune();
            return _stack.Count;
        }
    }

    /// <summary>The canvas the popups are created under (created on first use).</summary>
    public RectTransform Root
    {
        get
        {
            EnsureCanvas();
            return _root;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);

        EnsureCanvas();
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnActiveSceneChanged(Scene previous, Scene next) => HandleSceneChange();

    private void Update()
    {
        // The Input System maps the Android back button to the Escape key
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            HandleBack();
    }

    /// <summary>
    /// What the back button does: closes the popup on top if that option is on.
    /// True if it closed one (later, false could let the scene handle back, e.g. "quit game?").
    /// Public so it can be tested without simulating input.
    /// </summary>
    public bool HandleBack() => closeTopOnBack && CloseTop();

    /// <summary>A popup from the previous scene would point at objects that no longer exist, so they all close.</summary>
    public void HandleSceneChange() => CloseAll();

    // ---------- IPopupService ----------

    public T Open<T>(T prefab) where T : PopupView
    {
        if (prefab == null)
        {
            Debug.LogWarning("[PopupService] Tried to open a popup with no prefab assigned.");
            return null;
        }

        Prune();

        // Already open (e.g. the button was tapped twice): keep the one we have
        foreach (PopupView open in _stack)
        {
            if (open.SourcePrefab == prefab) return (T)open;
        }

        EnsureCanvas();

        T popup = Instantiate(prefab, _root, false);
        popup.name = prefab.name;
        Stretch(popup.transform as RectTransform);
        popup.transform.SetAsLastSibling();

        popup.Bind(this, prefab);
        _stack.Add(popup);
        popup.NotifyOpened();

        return popup;
    }

    public void Close(PopupView popup)
    {
        if (popup == null) return;

        Prune();
        int index = _stack.IndexOf(popup);
        if (index < 0) return;

        // From the top down to this one, so the popups opened from it go too
        for (int i = _stack.Count - 1; i >= index; i--)
        {
            PopupView closing = _stack[i];
            _stack.RemoveAt(i);
            closing.NotifyClosed();
            DestroyObject(closing.gameObject);
        }
    }

    public bool CloseTop()
    {
        PopupView top = Top;
        if (top == null) return false;

        Close(top);
        return true;
    }

    public void CloseAll()
    {
        Prune();
        if (_stack.Count > 0) Close(_stack[0]);
    }

    // ---------- Helpers ----------

    // Popups destroyed from outside (e.g. with their scene) leave empty entries
    private void Prune() => _stack.RemoveAll(p => p == null);

    private void EnsureCanvas()
    {
        if (_root != null) return;

        var canvasGo = new GameObject("PopupCanvas", typeof(RectTransform));
        canvasGo.layer = LayerMask.NameToLayer("UI");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = matchWidthOrHeight;

        // The scene's EventSystem uses this raycaster too, so popups take the taps (and block the board)
        canvasGo.AddComponent<GraphicRaycaster>();

        _root = (RectTransform)canvasGo.transform;
    }

    // The popup root fills the canvas: its dim covers the whole screen
    private static void Stretch(RectTransform rect)
    {
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    /// <summary>Destroy in play mode, DestroyImmediate in edit mode (EditMode tests).</summary>
    internal static void DestroyObject(GameObject target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
