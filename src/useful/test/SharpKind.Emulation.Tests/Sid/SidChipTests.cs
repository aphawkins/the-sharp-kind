// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Emulation.Sid;

namespace SharpKind.Emulation.Tests.Sid;

public class SidChipTests
{
    private const int Voice1Frequency = 0x00;
    private const int Voice1Control = 0x04;
    private const int Voice1AttackDecay = 0x05;
    private const int Voice1SustainRelease = 0x06;
    private const int ModeVolume = 0x18;

    // 440 Hz at the PAL clock: 440 * 2^24 / 985248.
    private const int Concert440 = 0x1D44;

    [Fact]
    public void VolumeZeroIsSilent()
    {
        // Arrange
        SidChip chip = new();
        PlaySaw(chip, 0x00);
        float[] buffer = new float[2 * SidChip.SampleRate / 10];

        // Act
        chip.Render(buffer);

        // Assert
        Assert.All(buffer, sample => Assert.Equal(0f, sample));
    }

    [Fact]
    public void AGatedSawSoundsAtItsFrequency()
    {
        // Arrange
        SidChip chip = new();
        PlaySaw(chip, 0x0F);
        float[] buffer = new float[2 * SidChip.SampleRate];

        // Act
        chip.Render(buffer);

        // Assert: a saw crosses zero upwards once a period.
        int crossings = 0;

        for (int i = 4; i < buffer.Length; i += 2)
        {
            if (buffer[i - 2] < 0 && buffer[i] >= 0)
            {
                crossings++;
            }
        }

        Assert.InRange(crossings, 436, 444);
    }

    [Fact]
    public void RenderingAsecondClocksTheRightNumberOfCycles()
    {
        // Arrange
        SidChip chip = new();
        float[] buffer = new float[2 * SidChip.SampleRate];

        // Act
        chip.Render(buffer);

        // Assert
        Assert.InRange(chip.Cycles, 985_248 - 30, 985_248 + 30);
    }

    [Fact]
    public void RewritingTheGateRestartsTheAttack()
    {
        // Arrange
        SidChip chip = new();
        chip.Write(Voice1AttackDecay, 0x00);
        chip.Write(Voice1SustainRelease, 0xF0);
        chip.Write(Voice1Control, 0x21);
        chip.Advance(9 * 255);
        int reached = chip.EnvelopeLevel(0);

        // Act
        chip.Write(Voice1Control, 0x20);
        chip.Advance(9 * 50);
        int released = chip.EnvelopeLevel(0);
        chip.Write(Voice1Control, 0x21);
        chip.Advance(9 * 10);

        // Assert
        Assert.Equal(255, reached);
        Assert.InRange(released, 200, 254);
        Assert.InRange(chip.EnvelopeLevel(0), released, 255);
    }

    [Fact]
    public void TheThirdVoiceCanBeReadBack()
    {
        // Arrange
        SidChip chip = new();
        chip.Write(0x0E + 5, 0x00);
        chip.Write(0x0E + 6, 0xF0);
        chip.Write(0x0E + 4, 0x11);

        // Act
        chip.Advance(9 * 100);

        // Assert
        Assert.InRange(chip.Read(0x1C), 95, 105);
    }

    [Fact]
    public void RegistersMirrorEveryThirtyTwoBytes()
    {
        // Arrange
        SidChip chip = new();
        chip.Write(0x0E + 5, 0x00);
        chip.Write(0x0E + 6, 0xF0);

        // Act
        chip.Write(0x20 + 0x0E + 4, 0x11);
        chip.Advance(9 * 20);

        // Assert
        Assert.True(chip.Read(0x1C) > 0);
    }

    [Fact]
    public void ALowPassFilterQuietensAHighNote()
    {
        // Arrange
        SidChip open = new();
        SidChip filtered = new();
        PlaySaw(open, 0x0F);
        PlaySaw(filtered, 0x1F);
        filtered.Write(0x15, 0x00);
        filtered.Write(0x16, 0x02);
        filtered.Write(0x17, 0x01);
        open.Write(0x0, 0x00);
        open.Write(0x1, 0x40);
        filtered.Write(0x0, 0x00);
        filtered.Write(0x1, 0x40);

        // Act
        float[] openBuffer = new float[2 * SidChip.SampleRate / 5];
        float[] filteredBuffer = new float[openBuffer.Length];
        open.Render(openBuffer);
        filtered.Render(filteredBuffer);

        // Assert
        Assert.True(Peak(filteredBuffer) < Peak(openBuffer) / 2);
    }

    [Fact]
    public void AFilteredNoteRingsDownToExactSilence()
    {
        // Arrange: a saw through a resonant low pass, then released.
        SidChip chip = new();
        PlaySaw(chip, 0x1F);
        chip.Write(0x15, 0x07);
        chip.Write(0x16, 0x20);
        chip.Write(0x17, 0xF1);
        float[] buffer = new float[2 * SidChip.SampleRate / 10];
        chip.Render(buffer);
        chip.Write(Voice1Control, 0x20);

        // Act
        for (int i = 0; i < 40; i++)
        {
            chip.Render(buffer);
        }

        // Assert
        Assert.All(buffer, sample => Assert.Equal(0f, sample));
    }

    [Fact]
    public void ASilentChipStillCountsItsCycles()
    {
        // Arrange
        SidChip chip = new();
        float[] buffer = new float[2 * SidChip.SampleRate];

        // Act
        chip.Render(buffer);

        // Assert
        Assert.InRange(chip.Cycles, (long)SidChip.ClockRate - 1, (long)SidChip.ClockRate + 1);
    }

    private static float Peak(float[] buffer) => buffer.Skip(buffer.Length / 2).Max(Math.Abs);

    private static void PlaySaw(SidChip chip, byte modeVolume)
    {
        chip.Write(Voice1Frequency, Concert440 & 0xFF);
        chip.Write(Voice1Frequency + 1, Concert440 >> 8);
        chip.Write(Voice1AttackDecay, 0x00);
        chip.Write(Voice1SustainRelease, 0xF0);
        chip.Write(ModeVolume, modeVolume);
        chip.Write(Voice1Control, 0x21);
    }
}
