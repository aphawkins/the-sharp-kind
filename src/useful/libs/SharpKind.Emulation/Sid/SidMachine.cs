// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Emulation.Cpu;

namespace SharpKind.Emulation.Sid;

/// <summary>
/// A C64 reduced to what a SID tune needs: a 6502, RAM, the SID, and a PAL frame timer. At the start of each 50 Hz frame
/// the machine calls the tune's play routine, then the chip runs on for the frame.
/// </summary>
public sealed class SidMachine
{
    private const int FrameCycles = 312 * 63;
    private const int InitCycles = 2_000_000;
    private const int PlayCycles = 100_000;
    private const byte FullVolume = 0x0F;
    private const int VolumeRegister = 0x18;

    private readonly double _samplesPerFrame = FrameCycles / SidChip.ClockRate * SidChip.SampleRate;

    private PsidFile? _tune;
    private double _samplesLeft;

    public SidMachine()
    {
        Chip = new SidChip();
        Bus = new C64Bus(Chip);
        Cpu = new Cpu6502(Bus);
        Chip.Write(VolumeRegister, FullVolume);
    }

    public SidChip Chip { get; }

    public C64Bus Bus { get; }

    public Cpu6502 Cpu { get; }

    /// <summary>Gets a value indicating whether a tune is being played.</summary>
    public bool IsPlaying => _tune is not null;

    /// <summary>Gets or sets a routine run at the start of each frame when no tune is playing.</summary>
    public Action? IdleFrame { get; set; }

    /// <summary>Loads a tune and runs its init routine.</summary>
    /// <param name="tune">The tune.</param>
    /// <param name="song">The song to play, 1 to <see cref="PsidFile.Songs"/>.</param>
    /// <exception cref="SharpKindException">The song does not exist.</exception>
    public void StartMusic(PsidFile tune, int song)
    {
        ArgumentNullException.ThrowIfNull(tune);

        if (song < 1 || song > tune.Songs)
        {
            throw new SharpKindException($"Song {song} is not in a tune of {tune.Songs}.");
        }

        for (int register = 0; register < VolumeRegister; register++)
        {
            Chip.Write(register, 0);
        }

        // A tune's own init may not set the volume, and the one a song before it left could be off.
        Chip.Write(VolumeRegister, FullVolume);

        Bus.Clear();
        Bus.Load(tune.LoadAddress, tune.Data.Span);
        Cpu.Call(tune.InitAddress, (byte)(song - 1), InitCycles);
        _tune = tune;
    }

    /// <summary>Stops the tune and silences the chip, leaving it at full volume with no filter.</summary>
    public void StopMusic()
    {
        _tune = null;

        for (int register = 0; register < VolumeRegister; register++)
        {
            Chip.Write(register, 0);
        }

        Chip.Write(VolumeRegister, FullVolume);
    }

    /// <summary>Runs a routine in the machine as JSR would.</summary>
    /// <param name="address">Where the routine starts.</param>
    /// <param name="a">The accumulator on entry.</param>
    /// <param name="x">The X register on entry.</param>
    /// <param name="y">The Y register on entry.</param>
    /// <returns>The cycles taken.</returns>
    public int Call(int address, byte a, byte x, byte y) => Cpu.Call(address, a, PlayCycles, x, y);

    /// <summary>Renders stereo frames at <see cref="SidChip.SampleRate"/>, running the frame timer as it goes.</summary>
    /// <param name="stereo">Interleaved left and right samples to fill.</param>
    public void Render(Span<float> stereo)
    {
        int total = stereo.Length / 2;
        int done = 0;

        while (done < total)
        {
            if (_samplesLeft <= 0)
            {
                RunFrame();
                _samplesLeft += _samplesPerFrame;
            }

            int count = Math.Min(total - done, (int)Math.Ceiling(_samplesLeft));

            Chip.Render(stereo.Slice(done * 2, count * 2));
            done += count;
            _samplesLeft -= count;
        }
    }

    private void RunFrame()
    {
        if (_tune is not null)
        {
            Cpu.Call(_tune.PlayAddress, 0, PlayCycles);
        }
        else
        {
            IdleFrame?.Invoke();
        }
    }
}
