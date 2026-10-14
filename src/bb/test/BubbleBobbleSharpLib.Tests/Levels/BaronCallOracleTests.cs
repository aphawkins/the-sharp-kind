// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The Barons' call (`$1621`), against the real game run on SharpKind.Emulation: the clock is run out a second time some frames
// into level 1 (interventions on `$2D` and `$2A`), and the game runs from the call to `$1693`. The port's hold has to last as
// many IRQs (`$08`) and ask for the level's tune on the same frame, once the round start's tune has ended (`$1685`).
[Trait("Level", "Integration")]
public sealed class BaronCallOracleTests
{
    private const int Call = 0x1621;
    private const int Return = 0x1693;
    private const int MusicStart = 0x05AD;
    private const int Seconds = 0x2A;
    private const int Hurry = 0x2D;
    private const int Counter = 0x08;

    private const byte RoundStartTune = 0x43;
    private const byte LevelCompleteTune = 0x27;

    [Theory]
    [InlineData(300)]
    [InlineData(777)]
    public void WaitsForTheRoundStartsTuneAsTheGameDoes(int frames)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(frames);
        machine.Poke(Hurry, 1);
        machine.Poke(Seconds, 1);
        RunTo(machine, Call);

        (List<(int Frame, byte Tune)> asked, int length) = Run(machine);

        EndRig rig = new();
        List<(int Frame, Song Song)> played = [];
        int frame = 0;
        rig.End.Play = song => played.Add((frame, song));
        rig.Entities.State[0] = 1;
        rig.Timer.Seconds = 0;
        rig.Timer.Hurry = 1;
        rig.End.Update();

        IGameHold hold = rig.End.Hold!;

        while (hold.Frame())
        {
            frame++;
        }

        Assert.Equal([(0, RoundStartTune), (107, LevelCompleteTune)], asked);
        Assert.Equal(length, frame);

        // The port asks inside the frame's call, before it is counted.
        Assert.Equal([(asked[1].Frame - 1, Song.LevelComplete)], played);
    }

    // From the call to `$1693`: the songs asked for, and the length, in IRQs.
    private static (List<(int Frame, byte Tune)> Asked, int Length) Run(Rebb64Machine machine)
    {
        byte counter = machine.Peek(Counter);
        long limit = machine.Cycles + (985_248L * 10);
        List<(int Frame, byte Tune)> asked = [];

        while (machine.Cpu.PC != Return)
        {
            if (machine.Cpu.PC == MusicStart)
            {
                asked.Add(((byte)(machine.Peek(Counter) - counter), machine.Cpu.Y));
            }

            machine.Step();
            Assert.True(machine.Cycles < limit, "The call did not return.");
        }

        return (asked, (byte)(machine.Peek(Counter) - counter));
    }

    private static void RunTo(Rebb64Machine machine, int pc)
    {
        long limit = machine.Cycles + (985_248L * 60);

        while (machine.Cpu.PC != pc)
        {
            machine.Step();
            Assert.True(machine.Cycles < limit, $"The game did not reach ${pc:X4}.");
        }
    }
}
