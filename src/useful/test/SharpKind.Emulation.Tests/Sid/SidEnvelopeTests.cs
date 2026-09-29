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
}
