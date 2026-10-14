// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// $32C1 against the real game run on SharpKind.Emulation.
//
// **An intervention gives the letters.** At the entry of `$32C1` in a level 1 pass, `$54` and `$55` are written, and the
// routine is then run to its return at `$0A5A` with the IRQ going. The port's holds are run a frame at a time with the clock.
// What is compared: the lives (`$045A`), the letters, `$21`, how many IRQs (`$08`) the holds last, and the clock (`$2A`,
// `$2B`). Two of the routine's steps run for as long as their work takes: `$2E79` draws the screen twice, and the screen of
// fruit draws again any random number past the foods. Each can take a frame more than it does the first time, which is what
// the port's hold takes; the clock is compared with the IRQs the game took over the port's run on.
[Trait("Level", "Integration")]
public sealed class ExtendBonusOracleTests
{
    private const int Lives = 0x045A;
    private const int Letters = 0x54;
    private const int Complete = 0x21;
    private const int Counter = 0x08;
    private const int Seconds = 0x2A;
    private const int Frames = 0x2B;
    private const int CpuBoundSteps = 2;

    [Theory]
    [InlineData(0x3F, 0x00)]
    [InlineData(0x00, 0x3F)]
    [InlineData(0x3F, 0x3F)]
    [InlineData(0x3E, 0x1F)]
    public void AwardsExactlyAsTheGameDoes(byte first, byte second)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunTo(0x32C1, 2_000_000);

        // The fill draws again past the foods, so it wants random numbers that go on.
        EndRig rig = new(new RandomSource(new Random(1)));
        int resets = 0;

        rig.Players.Lives[0] = machine.Peek(Lives);
        rig.Players.Lives[1] = machine.Peek(Lives + 1);
        rig.Players.ExtendLetters[0] = first;
        rig.Players.ExtendLetters[1] = second;
        rig.End.Begin();
        rig.Timer.Seconds = machine.Peek(Seconds);
        rig.Timer.Frames = machine.Peek(Frames);

        machine.Poke(Letters, first);
        machine.Poke(Letters + 1, second);
        machine.Poke(Complete, 0);

        byte counter = machine.Peek(Counter);
        int frames = 0;
        ExtendBonus extend = new(rig.Players, rig.End, () => resets++, rig.Random);

        for (extend.Update(); extend.Hold is ExtendBonus.ExtendHold hold; extend.Update())
        {
            extend.Hold = null;
            byte lives = rig.Players.Lives[hold.Player];

            while (hold.Frame())
            {
                frames++;
                rig.Timer.Advance();
                Assert.Equal(lives, rig.Players.Lives[hold.Player]);
            }

            hold.Finish(0);
        }

        machine.RunTo(0x0A5A, 40_000_000);

        Assert.Equal(machine.Peek(Lives), rig.Players.Lives[0]);
        Assert.Equal(machine.Peek(Lives + 1), rig.Players.Lives[1]);
        Assert.Equal(machine.Peek(Letters), rig.Players.ExtendLetters[0]);
        Assert.Equal(machine.Peek(Letters + 1), rig.Players.ExtendLetters[1]);
        Assert.Equal(machine.Peek(Complete), rig.End.Complete);
        int holds = frames / ExtendBonus.ExtendHold.Length;
        int over = (byte)(machine.Peek(Counter) - counter - frames);

        Assert.Equal(holds * ExtendBonus.ExtendHold.Length, frames);
        Assert.InRange(over, 0, holds * CpuBoundSteps);
        Assert.Equal(holds, resets);

        for (int frame = 0; frame < over; frame++)
        {
            rig.Timer.Advance();
        }

        Assert.Equal(machine.Peek(Seconds), rig.Timer.Seconds);
        Assert.Equal(machine.Peek(Frames), rig.Timer.Frames);
    }
}
