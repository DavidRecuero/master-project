using UnityEngine;

/// <summary>
/// Idle life for the main menu machine (MenuMachine prefab): the whole panel squishes a little
/// and the drop on each exit platform hops in turn, with the same squash and stretch as the
/// PrimaryButton letters. Pure visuals, unscaled time. The drops are plain Images for now;
/// a later interactive version can replace them without touching this loop.
/// </summary>
public class MenuMachineFx : MonoBehaviour
{
    [Tooltip("Panel that squishes (pivot at its bottom centre)")]
    [SerializeField] private RectTransform body;
    [Tooltip("Drops sitting on the exit platforms (pivot at their bottom centre)")]
    [SerializeField] private RectTransform[] platformDrops;

    public float cycle = 2.6f;
    [Tooltip("Delay between one platform drop and the next (seconds)")]
    public float dropDelay = 0.35f;
    [Tooltip("How high a drop hops (canvas units; 21 = 7 pt)")]
    public float hopHeight = 21f;

    private Vector2[] _dropRest;

    private void Awake()
    {
        _dropRest = new Vector2[platformDrops != null ? platformDrops.Length : 0];
        for (int i = 0; i < _dropRest.Length; i++)
        {
            if (platformDrops[i] != null) _dropRest[i] = platformDrops[i].anchoredPosition;
        }
    }

    private void OnDisable()
    {
        if (body != null) body.localScale = Vector3.one;
        for (int i = 0; i < _dropRest.Length; i++)
        {
            if (platformDrops[i] == null) continue;
            platformDrops[i].anchoredPosition = _dropRest[i];
            platformDrops[i].localScale = Vector3.one;
        }
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        if (body != null)
        {
            Vector2 s = SquishScale(Mathf.Repeat(time, cycle) / cycle);
            body.localScale = new Vector3(s.x, s.y, 1f);
        }

        for (int i = 0; i < _dropRest.Length; i++)
        {
            if (platformDrops[i] == null) continue;

            Vector3 pose = HopPose(Mathf.Repeat(time - i * dropDelay, cycle) / cycle);
            platformDrops[i].localScale = new Vector3(pose.x, pose.y, 1f);
            platformDrops[i].anchoredPosition = _dropRest[i] + new Vector2(0f, pose.z * hopHeight);
        }
    }

    /// <summary>Panel scale along the loop: rest, slightly flattened at 45%, slightly tall at 60%, rest.</summary>
    public static Vector2 SquishScale(float t01)
    {
        if (t01 < 0.45f) return Vector2.Lerp(Vector2.one, new Vector2(1.015f, 0.985f), Ease(t01 / 0.45f));
        if (t01 < 0.60f) return Vector2.Lerp(new Vector2(1.015f, 0.985f), new Vector2(0.992f, 1.01f), Ease((t01 - 0.45f) / 0.15f));
        return Vector2.Lerp(new Vector2(0.992f, 1.01f), Vector2.one, Ease((t01 - 0.60f) / 0.40f));
    }

    /// <summary>Drop pose along the loop: rest until 70%, up and stretched at 78%, squashed at 86%, rest.
    /// Returns (scaleX, scaleY, lift 0..1).</summary>
    public static Vector3 HopPose(float t01)
    {
        if (t01 <= 0.70f) return new Vector3(1f, 1f, 0f);
        if (t01 < 0.78f)
        {
            float k = Ease((t01 - 0.70f) / 0.08f);
            return new Vector3(Mathf.Lerp(1f, 0.94f, k), Mathf.Lerp(1f, 1.06f, k), k);
        }
        if (t01 < 0.86f)
        {
            float k = Ease((t01 - 0.78f) / 0.08f);
            return new Vector3(Mathf.Lerp(0.94f, 1.08f, k), Mathf.Lerp(1.06f, 0.92f, k), 1f - k);
        }
        float back = Ease((t01 - 0.86f) / 0.14f);
        return new Vector3(Mathf.Lerp(1.08f, 1f, back), Mathf.Lerp(0.92f, 1f, back), 0f);
    }

    private static float Ease(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
}
