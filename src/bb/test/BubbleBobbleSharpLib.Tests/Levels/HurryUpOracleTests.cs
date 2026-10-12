// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The HURRY UP! scroll (`$3AB8`), against the real game run on SharpKind.Emulation: the clock is run out some frames into level 1
// (an intervention on `$2A`), and the game runs from the scroll's call to its return (`$15F1`). The port's hold has to last as
// many IRQs (`$08`), ask for the same songs on the same frames, and leave the clock's frames (`$2B`) where the IRQ left them.
[Trait("Level", "Integration")]
public sealed class HurryUpOracleTests
{
    private const int Scroll = 0x3AB8;
    private const int Return = 0x15F1;
    private const int MusicStart = 0x05AD;
    private const int Seconds = 0x2A;
    private const int Frames = 0x2B;
    private const int Counter = 0x08;

    // Past `$3BB1`'s wait for `$A4`, the song playing, to be 0.
    private const int TuneEnded = 0x3BB5;

    // The `sta ($18),y`s that put the letters' shapes in their characters: `$3CD6`'s plain ones, and at row 11 `$3D1E`'s backed.
    private const int CopyLetters = 0x3B7F;
    private const int CopyBacked = 0x3B8F;

    private const byte FirstLetter = 0x37;
    private const byte LevelTile = 0x15;
    private const int Letters = 9;

    private const byte HurryUpTune = 0x3C;
    private const byte LevelCompleteTune = 0x27;

    [Theory]
    [InlineData(300)]
    [InlineData(777)]
    public void ScrollsForAsLongAsTheGameDoes(int frames)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(frames);
        machine.Poke(Seconds, 1);
        RunTo(machine, Scroll);

        byte clock = machine.Peek(Frames);
        Trace game = Run(machine);

        EndRig rig = new();
        List<(int Frame, Song Song)> played = [];
        List<string> shown = [Shown(null)];
        int frame = 0;
        rig.End.Play = song => played.Add((frame, song));
        rig.Timer.Seconds = 0;
        rig.Timer.Frames = clock;
        rig.Timer.Hurry = 0;
        rig.End.Update();

        LevelEnd.HurryHold hold = rig.End.Hurrying!;

        while (hold.Frame())
        {
            shown.Add(Shown(hold));
            frame++;
            rig.Timer.Advance();
        }

        hold.Finish(0);

        Assert.Equal([(15, HurryUpTune), (101, LevelCompleteTune)], game.Asked);
        Assert.Equal(86, game.TuneEnd - game.Asked[0].Frame);
        Assert.Equal(game.Length, frame);

        // The port asks inside the frame's call, before it is counted.
        Assert.Equal(game.Asked.Select(a => a.Frame - 1), played.Select(p => p.Frame));
        Assert.Equal([Song.HurryUp, Song.LevelComplete], played.Select(p => p.Song));
        Assert.Equal(machine.Peek(Frames), rig.Timer.Frames);
        Assert.Equal(1, rig.Timer.Hurry);
        Assert.Equal(0x0A, rig.Timer.Seconds);
        Assert.Equal(string.Join(Environment.NewLine, game.Screens), string.Join(Environment.NewLine, shown.Take(game.Screens.Count)));
    }

    // From the scroll's call to its return: the songs asked for, the tune's end and the length, in IRQs, and what each IRQ found
    // on the screen.
    private static Trace Run(Rebb64Machine machine)
    {
        byte counter = machine.Peek(Counter);
        long limit = machine.Cycles + (985_248L * 10);
        Trace trace = new();
        bool backed = false;
        int sampled = 0;

        while (machine.Cpu.PC != Return)
        {
            switch (machine.Cpu.PC)
            {
                case MusicStart:
                    trace.Asked.Add((Irqs(machine, counter), machine.Cpu.Y));
                    break;
                case TuneEnded:
                    trace.TuneEnd = Irqs(machine, counter);
                    break;
                case CopyLetters:
                    backed = false;
                    break;
                case CopyBacked:
                    backed = true;
                    break;
            }

            // What the routine has left on the screen after each wait, by the time the next IRQ comes.
            if (Irqs(machine, counter) == sampled)
            {
                if (sampled > 0)
                {
                    trace.Screens.Add(Screen(machine, backed));
                }

                sampled++;
                Underlay(machine);
            }

            machine.Step();
            Assert.True(machine.Cycles < limit, "The scroll did not return.");
        }

        trace.Length = Irqs(machine, counter);
        return trace;
    }

    // Row 11 under the level's tile until the text reaches it, so that its backing shows (an intervention: level 1 has nothing
    // there, and the screen is put back from its copy as the game goes).
    private static void Underlay(Rebb64Machine machine)
    {
        int strip = Strip(machine.Ram);

        if (strip < 0 || ((strip & 0x3FF) / 40) <= 11)
        {
            return;
        }

        int under = strip - ((((strip & 0x3FF) / 40) - 11) * 40);

        for (int letter = 0; letter < Letters; letter++)
        {
            machine.Poke(under + letter, LevelTile);
        }
    }

    private static string Shown(LevelEnd.HurryHold? hold)
        => hold?.Row is int row ? $"row {row} colour {hold.ColourRam:X2} backed {hold.Backed}" : "none";

    // The row with screen codes $37-$3F in columns 12-20, its colour RAM, and whether the characters were last given the backed
    // letters.
    private static string Screen(Rebb64Machine machine, bool backed)
    {
        ReadOnlySpan<byte> ram = machine.Ram;
        int strip = Strip(ram);

        if (strip < 0)
        {
            return "none";
        }

        int offset = strip & 0x3FF;

        return $"row {offset / 40} colour {machine.PeekColour(offset) & 0x0F:X2} backed {backed}";
    }

    // Where the strip is in the RAM, on a screen that starts on a 1 KB boundary, or -1.
    private static int Strip(ReadOnlySpan<byte> ram)
    {
        for (int address = 0; address < ram.Length - Letters; address++)
        {
            int offset = address & 0x3FF;

            if (offset % 40 is 12 && offset / 40 < 25 && ram[address] == FirstLetter && IsStrip(ram[address..]))
            {
                return address;
            }
        }

        return -1;
    }

    private static bool IsStrip(ReadOnlySpan<byte> ram)
    {
        for (int letter = 0; letter < Letters; letter++)
        {
            if (ram[letter] != FirstLetter + letter)
            {
                return false;
            }
        }

        return true;
    }

    // The IRQs since the scroll's call, by their frame counter.
    private static int Irqs(Rebb64Machine machine, byte counter) => (byte)(machine.Peek(Counter) - counter);

    private static void RunTo(Rebb64Machine machine, int pc)
    {
        long limit = machine.Cycles + (985_248L * 60);

        while (machine.Cpu.PC != pc)
        {
            machine.Step();
            Assert.True(machine.Cycles < limit, $"The game did not reach ${pc:X4}.");
        }
    }

    private sealed class Trace
    {
        internal List<(int Frame, byte Tune)> Asked { get; } = [];

        internal int? TuneEnd { get; set; }

        internal int Length { get; set; }

        internal List<string> Screens { get; } = [];
    }
}
