using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Idle animation of the PrimaryButton prefab (Play, Next level, Retry), all on one loop:
/// the hazard stripes of the frame march like a conveyor belt, every few seconds the letters
/// hop one after another (same squash and stretch as the drops) and a shine sweeps the face.
/// Stops while the button is not interactable. Uses unscaled time, so it also runs on a paused game.
/// </summary>
public class PrimaryButtonFx : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private RawImage stripes;
    [SerializeField] private RectTransform shine;
    [Tooltip("Optional. While it is not interactable the button stays still.")]
    [SerializeField] private Selectable selectable;

    [Header("Loop")]
    public bool animate = true;
    [Tooltip("Seconds of one full loop (letters hop + shine)")]
    public float cycle = 3.2f;

    [Header("Letters")]
    [Tooltip("Delay between one letter and the next (seconds)")]
    public float letterDelay = 0.06f;
    [Tooltip("How high a letter hops (canvas units; 21 = 7 pt)")]
    public float hopHeight = 21f;

    [Header("Stripes")]
    [Tooltip("Size of one stripe tile on screen (canvas units). The texture is 64 px.")]
    public float tileSize = 64f;
    [Tooltip("Tiles per second")]
    public float stripeSpeed = 0.83f;

    private float _stripeOffset;
    private Vector2 _shineRest;
    private bool _atRest;

    private void Awake()
    {
        if (shine != null) _shineRest = shine.anchoredPosition;
    }

    private void OnDisable()
    {
        ResetPose();
    }

    private void LateUpdate()
    {
        bool running = animate && (selectable == null || selectable.IsInteractable());
        if (!running)
        {
            ResetPose();
            return;
        }

        _atRest = false;
        float time = Time.unscaledTime;
        UpdateStripes(Time.unscaledDeltaTime);
        UpdateShine(time);
        UpdateLetters(time);
    }

    // ---------- Stripes ----------

    private void UpdateStripes(float deltaTime)
    {
        if (stripes == null) return;

        Rect r = stripes.rectTransform.rect;
        _stripeOffset = Mathf.Repeat(_stripeOffset - stripeSpeed * deltaTime, 1f);
        stripes.uvRect = new Rect(_stripeOffset, 0f, r.width / tileSize, r.height / tileSize);
    }

    // ---------- Shine ----------

    private void UpdateShine(float time)
    {
        if (shine == null || !(shine.parent is RectTransform face)) return;

        float t = Mathf.Repeat(time, cycle) / cycle;
        float k = ShineProgress(t);
        float travel = face.rect.width * 0.5f + shine.rect.width;
        shine.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, k), _shineRest.y);
    }

    /// <summary>0 before 62% of the loop, 1 after 82%, eased in between.</summary>
    public static float ShineProgress(float t01)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.82f, t01));
    }

    // ---------- Letters ----------

    private void UpdateLetters(float time)
    {
        if (label == null) return;

        label.ForceMeshUpdate();
        TMP_TextInfo info = label.textInfo;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible) continue;

            float t = Mathf.Repeat(time - i * letterDelay, cycle) / cycle;
            Vector3 pose = HopPose(t);   // x = scale X, y = scale Y, z = lift (0..1)
            if (pose.z == 0f && pose.x == 1f && pose.y == 1f) continue;

            Vector3[] v = info.meshInfo[c.materialReferenceIndex].vertices;
            int first = c.vertexIndex;
            Vector3 pivot = new Vector3((v[first].x + v[first + 2].x) * 0.5f, c.baseLine, 0f);
            Vector3 lift = new Vector3(0f, pose.z * hopHeight, 0f);

            for (int k = 0; k < 4; k++)
            {
                Vector3 local = v[first + k] - pivot;
                v[first + k] = pivot + new Vector3(local.x * pose.x, local.y * pose.y, 0f) + lift;
            }
        }

        label.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    /// <summary>
    /// Letter pose along the loop (same keyframes as the mockup): rest until 50%, stretched up at 57%,
    /// squashed on landing at 64%, rest again from 72%. Returns (scaleX, scaleY, lift 0..1).
    /// </summary>
    public static Vector3 HopPose(float t01)
    {
        if (t01 <= 0.50f || t01 >= 0.72f) return new Vector3(1f, 1f, 0f);

        if (t01 < 0.57f)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.50f, 0.57f, t01));
            return new Vector3(Mathf.Lerp(1f, 0.94f, k), Mathf.Lerp(1f, 1.10f, k), k);
        }
        if (t01 < 0.64f)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.57f, 0.64f, t01));
            return new Vector3(Mathf.Lerp(0.94f, 1.08f, k), Mathf.Lerp(1.10f, 0.92f, k), 1f - k);
        }

        float back = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.64f, 0.72f, t01));
        return new Vector3(Mathf.Lerp(1.08f, 1f, back), Mathf.Lerp(0.92f, 1f, back), 0f);
    }

    private void ResetPose()
    {
        if (_atRest) return;
        _atRest = true;

        if (shine != null) shine.anchoredPosition = new Vector2(-10000f, _shineRest.y);
        if (label != null) label.ForceMeshUpdate();
    }
}
