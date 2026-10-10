// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.Sid;

namespace SharpKind.Audio.Tests;

// Each expectation is traced by hand from NOISE, SOUX9 and SOINT in C64 Elite's elite-source.asm, with the real table rows.
public sealed class SidEffectEngineTests
{
    // Voice 1's registers are 0-6, voice 2's 7-13 and voice 3's 14-20; register 4 of each is its control.
    private const int Voice1Control = 4;
    private const int Voice2Control = 11;
    private const int Voice3Control = 18;

    private readonly IReadOnlyDictionary<string, IReadOnlyList<SidEffectStep>> _sounds
        = SidEffectFile.Parse(SidFixtures.EffectFile, "fixture").Sounds;

    private readonly List<(int Register, byte Value)> _writes = [];
    private readonly SidEffectEngine _engine;

    public SidEffectEngineTests() => _engine = new((register, value) => _writes.Add((register, value)));

    [Fact]
    public void EffectsOnAnIdleChipTakeVoices3Then2Then1()
    {
        _engine.Trigger(_sounds["Beep"]);
        _engine.Trigger(_sounds["Boop"]);
        _engine.Trigger(_sounds["Launch"]);

        Dictionary<int, byte> registers = Tick();

        Assert.Equal(0x11, registers[Voice3Control]); // sfxbeep
        Assert.Equal(0x11, registers[Voice2Control]); // sfxboop
        Assert.Equal(0x81, registers[Voice1Control]); // sfxwhosh
    }

    [Fact]
    public void ABeepSetsItsVoiceOnTheFirstTickAndGatesOffOnTheSixth()
    {
        _engine.Trigger(_sounds["Beep"]);

        Assert.Equal(
            new Dictionary<int, byte> { [14] = 0x00, [15] = 0x3C, [16] = 0, [17] = 2, [18] = 0x11, [19] = 0x00, [20] = 0xF0 },
            Tick());

        for (int tick = 2; tick <= 5; tick++)
        {
            Assert.Empty(Tick());
        }

        Assert.Equal(new Dictionary<int, byte> { [Voice3Control] = 0x10 }, Tick());
        Assert.Empty(Tick());
    }

    [Fact]
    public void APulseLaserFallsTwoInFrequencyEachTickAndFadesEveryFourth()
    {
        _engine.Trigger(_sounds["Pulse"]);

        List<int> frequencies = [];
        List<(int Tick, byte SustainRelease)> fades = [];

        for (int tick = 1; tick <= 9; tick++)
        {
            Dictionary<int, byte> registers = Tick();
            frequencies.Add((registers[15] << 2) | (registers[14] >> 6));

            if (tick > 1 && registers.TryGetValue(20, out byte sustainRelease))
            {
                fades.Add((tick, sustainRelease));
            }
        }

        Assert.Equal([69, 67, 65, 63, 61, 59, 57, 55, 53], frequencies);
        Assert.Equal([(5, 0xC1), (9, 0xB1)], fades);
    }

    [Fact]
    public void ThePulseWidthAlternatesBetween2And6()
    {
        _engine.Trigger(_sounds["Pulse"]);

        Assert.Equal([2, 6, 2, 6], Enumerable.Range(0, 4).Select(_ => Tick()[17]));
    }

    [Fact]
    public void AnEffectWithAnEvenPriorityTakesBackItsOwnVoice()
    {
        _engine.Trigger(_sounds["Beep"]);
        Tick();

        _engine.Trigger(_sounds["Beep"]);

        Assert.Equal(0x11, Tick()[Voice3Control]);
    }

    [Fact]
    public void AnEffectWithAnOddPriorityTakesAnotherVoice()
    {
        _engine.Trigger(_sounds["Launch"]);
        Tick();

        _engine.Trigger(_sounds["Launch"]);

        Assert.Equal(0x81, Tick()[Voice2Control]);
    }

    [Fact]
    public void ALowPriorityEffectIsDroppedWhileEveryVoiceHoldsAHigherOne()
    {
        _engine.Trigger(_sounds["Ecm"]);
        _engine.Trigger(_sounds["Launch"]);
        _engine.Trigger(_sounds["Pulse"]);
        Tick();

        _engine.Trigger(_sounds["Faint"]);
        Dictionary<int, byte> registers = Tick();

        Assert.DoesNotContain(Voice1Control, registers.Keys);
        Assert.DoesNotContain(Voice2Control, registers.Keys);
        Assert.DoesNotContain(Voice3Control, registers.Keys);
    }

    [Fact]
    public void HyperspaceLayersItsSecondDriveSoundOnTheNextTick()
    {
        _engine.Trigger(_sounds["Hyperspace"]);

        Dictionary<int, byte> first = Tick();
        Assert.Equal(0x41, first[Voice3Control]); // sfxhyp1
        Assert.Equal(0xF5, first[20]); // its sustain/release from NOISE2
        Assert.Equal(0x81, first[Voice2Control]); // sfxwhosh
        Assert.DoesNotContain(Voice1Control, first.Keys);

        Assert.Equal(0x41, Tick()[Voice1Control]); // sfxhyp1 again, layered
    }

    [Fact]
    public void FlushGatesEveryPlayingVoiceOffOnTheNextTick()
    {
        _engine.Trigger(_sounds["Beep"]);
        _engine.Trigger(_sounds["Boop"]);
        _engine.Trigger(_sounds["Launch"]);
        Tick();

        _engine.Flush();

        Assert.Equal(new Dictionary<int, byte> { [Voice1Control] = 0x80, [Voice2Control] = 0x10, [Voice3Control] = 0x10 }, Tick());
    }

    [Fact]
    public void FlushDropsDelayedSteps()
    {
        _engine.Trigger(_sounds["Hyperspace"]);
        _engine.Flush();
        Tick();

        Assert.DoesNotContain(Voice1Control, Tick().Keys);
    }

    [Fact]
    public void AnEmptySoundIsSilence()
    {
        _engine.Trigger(_sounds["Dock"]);

        Assert.Empty(Tick());
    }

    // One tick's writes, the last value written to each register.
    private Dictionary<int, byte> Tick()
    {
        _writes.Clear();
        _engine.Tick();

        Dictionary<int, byte> registers = [];
        foreach ((int register, byte value) in _writes)
        {
            registers[register] = value;
        }

        return registers;
    }
}
