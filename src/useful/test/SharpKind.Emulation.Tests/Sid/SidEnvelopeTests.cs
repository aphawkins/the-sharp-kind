// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Emulation.Sid;

namespace SharpKind.Emulation.Tests.Sid;

public class SidEnvelopeTests
{
    [Fact]
    public void AttackZeroReachesTheTopInAboutTwoThousandThreeHundredCycles()
    {
        // Arrange
        SidEnvelope envelope = new() { AttackRate = 0, DecayRate = 0, SustainLevel = 15, ReleaseRate = 0 };
        envelope.SetGate(true);
        int cycles = 0;

        // Act
        while (envelope.Level < 255)
        {
            envelope.Clock();
            cycles++;
        }

        // Assert
        Assert.Equal(255 * 9, cycles);
    }

    [Fact]
    public void TheEnvelopeHoldsAtTheSustainLevel()
    {
        // Arrange: sustain 8 is $88.
        SidEnvelope envelope = new() { AttackRate = 0, DecayRate = 0, SustainLevel = 8, ReleaseRate = 0 };
        envelope.SetGate(true);

        // Act
        for (int i = 0; i < 100_000; i++)
        {
            envelope.Clock();
        }

        // Assert
        Assert.Equal(0x88, envelope.Level);
    }

    [Fact]
    public void ReleaseFallsToZero()
    {
        // Arrange
        SidEnvelope envelope = new() { AttackRate = 0, DecayRate = 0, SustainLevel = 15, ReleaseRate = 0 };
        envelope.SetGate(true);

        for (int i = 0; i < 9 * 300; i++)
        {
            envelope.Clock();
        }

        // Act
        envelope.SetGate(false);

        for (int i = 0; i < 100_000; i++)
        {
            envelope.Clock();
        }

        // Assert
        Assert.Equal(0, envelope.Level);
    }

    [Fact]
    public void GatingAgainFromMidReleaseGoesBackToAttack()
    {
        // Arrange
        SidEnvelope envelope = new() { AttackRate = 0, DecayRate = 0, SustainLevel = 15, ReleaseRate = 0 };
        envelope.SetGate(true);

        for (int i = 0; i < 9 * 300; i++)
        {
            envelope.Clock();
        }

        envelope.SetGate(false);

        for (int i = 0; i < 9 * 100; i++)
        {
            envelope.Clock();
        }

        int falling = envelope.Level;

        // Act
        envelope.SetGate(true);

        for (int i = 0; i < 9 * 20; i++)
        {
            envelope.Clock();
        }

        // Assert
        Assert.True(envelope.Level > falling);
    }

    // A silent envelope's counters still move, and they decide when the next attack's first step and the next release's
    // steps fall, so Advance has to leave them exactly where clocking would.
    [Theory]
    [InlineData(0, 22)]
    [InlineData(9, 23)]
    [InlineData(15, 22)]
    [InlineData(3, 100_000)]
    public void AdvanceLeavesASilentEnvelopeWhereClockingWould(int releaseRate, int cycles)
    {
        // Arrange: released to silence at a slow rate, then given a faster one, so the rate counter is past its period.
        SidEnvelope advanced = Silenced();
        SidEnvelope clocked = Silenced();
        advanced.ReleaseRate = releaseRate;
        clocked.ReleaseRate = releaseRate;

        // Act
        for (int block = 0; block < 20; block++)
        {
            advanced.Advance(cycles);
            for (int i = 0; i < cycles; i++)
            {
                clocked.Clock();
            }
        }

        // Assert: the attack and release that follow step on the same cycles.
        Assert.True(advanced.IsSilent);
        Assert.Equal(Levels(clocked), Levels(advanced));
    }

    [Fact]
    public void AnAttackingEnvelopeIsNotSilent()
    {
        // Arrange
        SidEnvelope envelope = new() { AttackRate = 15 };

        // Act
        envelope.SetGate(true);

        // Assert
        Assert.Equal(0, envelope.Level);
        Assert.False(envelope.IsSilent);
    }

    private static SidEnvelope Silenced()
    {
        SidEnvelope envelope = new() { AttackRate = 0, DecayRate = 0, SustainLevel = 15, ReleaseRate = 15 };
        envelope.SetGate(true);
        while (envelope.Level < 255)
        {
            envelope.Clock();
        }

        envelope.SetGate(false);
        while (envelope.Level > 0)
        {
            envelope.Clock();
        }

        return envelope;
    }

    private static int[] Levels(SidEnvelope envelope)
    {
        List<int> levels = [];
        envelope.SetGate(true);
        for (int i = 0; i < 5000; i++)
        {
            envelope.Clock();
            levels.Add(envelope.Level);
        }

        envelope.SetGate(false);
        for (int i = 0; i < 300_000; i++)
        {
            envelope.Clock();
            levels.Add(envelope.Level);
        }

        return [.. levels];
    }
}
