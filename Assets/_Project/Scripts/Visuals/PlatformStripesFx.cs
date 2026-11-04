using UnityEngine;

/// <summary>
/// Moves the hazard stripes of an exit platform like a conveyor belt (same idea as the Play button),
/// by playing a short flipbook: each frame is the active platform with the stripes shifted a bit,
/// and the last frame joins the first one, so the loop has no jump.
/// PipeView adds it to the platform and turns it on only while the pipe has items to collect.
/// Every platform uses the same clock, so all the belts on the board move in step.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlatformStripesFx : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Sprite[] _frames;
    private float _framesPerSecond = 20f;

    public bool IsPlaying { get; private set; }

    public void Initialize(SpriteRenderer target, Sprite[] frames, float framesPerSecond)
    {
        _renderer = target;
        _frames = frames;
        _framesPerSecond = framesPerSecond;
    }

    /// <summary>True if there are frames to play (otherwise PipeView keeps the still sprite).</summary>
    public bool HasFrames => _frames != null && _frames.Length > 0;

    /// <summary>Starts or stops the belt. Starting shows the right frame at once (no still frame first).</summary>
    public void SetPlaying(bool playing)
    {
        IsPlaying = playing && HasFrames;
        enabled = IsPlaying;
        if (IsPlaying) ShowFrame(Time.time);
    }

    private void Update()
    {
        ShowFrame(Time.time);
    }

    private void ShowFrame(float time)
    {
        if (_renderer == null || !HasFrames) return;

        Sprite frame = _frames[FrameAt(time, _framesPerSecond, _frames.Length)];
        if (frame != null && _renderer.sprite != frame) _renderer.sprite = frame;
    }

    /// <summary>Frame to show at that time. Game time: the belt stops with the game when it is paused.</summary>
    public static int FrameAt(float time, float framesPerSecond, int frameCount)
    {
        if (frameCount <= 0) return 0;
        if (framesPerSecond <= 0f) return 0;

        int frame = Mathf.FloorToInt(time * framesPerSecond) % frameCount;
        return frame < 0 ? frame + frameCount : frame;
    }
}
