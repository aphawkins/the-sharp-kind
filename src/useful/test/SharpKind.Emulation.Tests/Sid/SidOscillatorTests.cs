// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Emulation.Sid;

namespace SharpKind.Emulation.Tests.Sid;

public class SidOscillatorTests
{
    [Fact]
    public void ASawRepeatsOnceEveryTwoToTheTwentyFourthOverTheFrequency()
    {
        // Arrange
        SidOscillator oscillator = new() { Frequency = 0x1000, Control = SidOscillator.SawBit };
        int wraps = 0;

        // Act: 2^24 / 2^12 = 4096 cycles a period.
        for (int i = 0; i < 4096 * 5; i++)
        {
            oscillator.Clock();

            if (oscillator.Output(false) == 0)
            {
                wraps++;
            }
        }

        // Assert
        Assert.Equal(5, wraps);
    }

    [Fact]
    public void ThePulseSpendsPulseWidthOverFourThousandAndNinetySixLow()
    {
        // Arrange: a quarter width.
        SidOscillator oscillator = new() { Frequency = 0x1000, PulseWidth = 0x400, Control = SidOscillator.PulseBit };
        int high = 0;

        // Act
        for (int i = 0; i < 4096; i++)
        {
            oscillator.Clock();
            high += oscillator.Output(false) == 0xFFF ? 1 : 0;
        }

        // Assert
        Assert.InRange(high, 3072 - 2, 3072 + 2);
    }

    [Fact]
    public void TheTestBitHoldsTheAccumulatorAtZero()
    {
        // Arrange
        SidOscillator oscillator = new() { Frequency = 0x1000 };
        oscillator.WriteControl(SidOscillator.SawBit | SidOscillator.TestBit);

        // Act
        for (int i = 0; i < 1000; i++)
        {
            oscillator.Clock();
        }

        // Assert
        Assert.Equal(0, oscillator.Output(false));
    }

    [Fact]
    public void NoiseChangesOnlyWhenTheClockBitRises()
    {
        // Arrange: bit 19 rises every 2^20 / 2^12 = 256 cycles at this frequency.
        SidOscillator oscillator = new() { Frequency = 0x1000, Control = SidOscillator.NoiseBit };
        int changes = 0;
        int last = oscillator.Output(false);

        // Act
        for (int i = 0; i < 4096; i++)
        {
            oscillator.Clock();

            int now = oscillator.Output(false);
            changes += now != last ? 1 : 0;
            last = now;
        }

        // Assert
        Assert.InRange(changes, 1, 16);
    }

    [Fact]
    public void SyncRestartsTheAccumulatorWhenTheSourceWraps()
    {
        // Arrange
        SidOscillator oscillator = new() { Frequency = 0x0100, Control = SidOscillator.SawBit | SidOscillator.SyncBit };

        for (int i = 0; i < 100; i++)
        {
            oscillator.Clock();
        }

        // Act
        oscillator.Synchronise(true);

        // Assert
        Assert.Equal(0, oscillator.Output(false));
    }

    [Fact]
    public void TheTriangleFoldsAtHalfWay()
    {
        // Arrange
        SidOscillator oscillator = new() { Frequency = 0x0800, Control = SidOscillator.TriangleBit };
        int peak = 0;

        // Act
        for (int i = 0; i < 4096; i++)
        {
            oscillator.Clock();
            peak = Math.Max(peak, oscillator.Output(false));
        }

        // Assert
        Assert.InRange(peak, 0xFF0, 0xFFF);
    }
}
