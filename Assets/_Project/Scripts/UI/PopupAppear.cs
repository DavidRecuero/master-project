using System.Collections;
using UnityEngine;

/// <summary>
/// Entrance of a popup every time it is shown: the dim fades in and the card pops
/// (grows past its size and settles). Put it on the popup root; it runs on unscaled time.
/// </summary>
public class PopupAppear : MonoBehaviour
{
    [SerializeField] private CanvasGroup dim;
    [SerializeField] private RectTransform card;

    public float fadeDuration = 0.18f;
    public float popDuration = 0.35f;
    [Tooltip("Scale the card starts from")]
    public float startScale = 0.7f;

    private Coroutine _routine;

    private void OnEnable()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Play());
    }

    private void OnDisable()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        Apply(1f, 1f);
    }

    private IEnumerator Play()
    {
        float t = 0f;
        float total = Mathf.Max(fadeDuration, popDuration);

        while (t < total)
        {
            t += Time.unscaledDeltaTime;
            float fade = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeDuration));
            float pop = Mathf.Clamp01(t / Mathf.Max(0.01f, popDuration));
            Apply(fade, Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(pop)));
            yield return null;
        }

        Apply(1f, 1f);
        _routine = null;
    }

    private void Apply(float alpha, float scale)
    {
        if (dim != null) dim.alpha = alpha;
        if (card != null) card.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>Goes a little past 1 before settling (overshoot ~8%).</summary>
    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        t = Mathf.Clamp01(t) - 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }
}
