using NUnit.Framework;

public class HapticsServiceTests
{
    private FakeHapticsBackend _backend;
    private FakeStorageProvider _storage;
    private HapticsService _service;
    private float _now;

    [SetUp]
    public void SetUp()
    {
        _backend = new FakeHapticsBackend();
        _storage = new FakeStorageProvider();
        _now = 0f;
        _service = new HapticsService(_backend, new HapticsSettingsStore(_storage), () => _now);
    }

    [Test]
    public void Play_SendsThePresetToTheBackend()
    {
        _service.Play(HapticType.MediumImpact);

        Assert.AreEqual(1, _backend.Played.Count);
        Assert.AreSame(HapticPresets.Get(HapticType.MediumImpact), _backend.Played[0]);
    }

    [Test]
    public void Play_None_DoesNothing()
    {
        _service.Play(HapticType.None);
        Assert.AreEqual(0, _backend.Played.Count);
    }

    [Test]
    public void Disabled_DoesNotVibrate()
    {
        _service.Enabled = false;

        _service.Play(HapticType.HeavyImpact);

        Assert.AreEqual(0, _backend.Played.Count);
    }

    [Test]
    public void Disabling_StopsTheCurrentVibration()
    {
        _service.Enabled = false;
        Assert.AreEqual(1, _backend.CancelCalls);
    }

    [Test]
    public void Unsupported_DoesNotVibrate()
    {
        _backend.IsSupported = false;

        _service.Play(HapticType.HeavyImpact);

        Assert.IsFalse(_service.IsSupported);
        Assert.AreEqual(0, _backend.Played.Count);
    }

    [Test]
    public void Enabled_IsSaved()
    {
        _service.Enabled = false;

        var reloaded = new HapticsService(_backend, new HapticsSettingsStore(_storage), () => _now);
        Assert.IsFalse(reloaded.Enabled);
    }

    [Test]
    public void SameType_WithinItsCooldown_PlaysOnce()
    {
        _service.Play(HapticType.Selection);
        _now = HapticsService.CooldownOf(HapticType.Selection) / 2f;
        _service.Play(HapticType.Selection);

        Assert.AreEqual(1, _backend.Played.Count);
    }

    [Test]
    public void SameType_AfterItsCooldown_PlaysAgain()
    {
        _service.Play(HapticType.Selection);
        _now = 1f;
        _service.Play(HapticType.Selection);

        Assert.AreEqual(2, _backend.Played.Count);
    }

    [Test]
    public void WeakerType_DoesNotCutAStrongerPatternThatIsStillPlaying()
    {
        _service.Play(HapticType.Success);
        _now = 0.05f; // Success lasts longer than this
        _service.Play(HapticType.Selection);

        Assert.AreEqual(1, _backend.Played.Count);
    }

    [Test]
    public void WeakerType_PlaysOnceTheStrongerPatternHasEnded()
    {
        _service.Play(HapticType.Success);
        _now = HapticPresets.Get(HapticType.Success).TotalMilliseconds / 1000f + 0.01f;
        _service.Play(HapticType.Selection);

        Assert.AreEqual(2, _backend.Played.Count);
    }

    [Test]
    public void StrongerType_CanCutAWeakerOne()
    {
        _service.Play(HapticType.MediumImpact);
        _service.Play(HapticType.Failure);

        Assert.AreEqual(2, _backend.Played.Count);
        Assert.AreSame(HapticPresets.Get(HapticType.Failure), _backend.Played[1]);
    }
}
