using UnityEngine;

/// <summary>
/// Counts matches made in quick succession and turns the count into a pitch: each match inside
/// the combo window sounds a little higher than the last one, up to a cap. Pure C#, the caller passes the time.
/// </summary>
public class ComboTracker
{
    private readonly float _window;
    private readonly int _maxSteps;
    private readonly float _semitonesPerStep;

    private float _lastMatchTime = float.NegativeInfinity;

    /// <summary>0 for the first match, 1 for the second in a row, and so on (capped at maxSteps).</summary>
    public int Step { get; private set; }

    /// <param name="window">Seconds after a match during which the next one counts as a combo.</param>
    /// <param name="maxSteps">Highest step, so the pitch never runs away.</param>
    /// <param name="semitonesPerStep">How much higher each step sounds. 2 = a whole tone.</param>
    public ComboTracker(float window = 2.5f, int maxSteps = 4, float semitonesPerStep = 2f)
    {
        _window = window;
        _maxSteps = maxSteps;
        _semitonesPerStep = semitonesPerStep;
    }

    /// <summary>Registers a match and returns the pitch multiplier to play it with.</summary>
    public float RegisterMatch(float now)
    {
        Step = now - _lastMatchTime <= _window ? Mathf.Min(Step + 1, _maxSteps) : 0;
        _lastMatchTime = now;
        return PitchForStep(Step);
    }

    public float PitchForStep(int step) => Mathf.Pow(2f, step * _semitonesPerStep / 12f);

    public void Reset()
    {
        Step = 0;
        _lastMatchTime = float.NegativeInfinity;
    }
}
