// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// $32C1 against the real game run on SharpKind.Emulation.
//
// **An intervention gives the letters.** At the entry of `$32C1` in a level 1 pass, `$54` and `$55` are written, and the
// routine is then run to its return at `$0A5A` with the IRQ going, since its fanfare and screen of fruit wait for frames.
// What is compared is what the port models: the lives (`$045A`), the letters, and `$21`. The port does not model the
// pictures, the fanfare, or the five seconds the game spends (see `ExtendBonus`).
[Trait("Level", "Integration")]
public sealed class ExtendBonusOracleTests
{
    private const int Lives = 0x045A;
    private const int Letters = 0x54;
    private const int Complete = 0x21;

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

        EndRig rig = new();

        rig.Players.Lives[0] = machine.Peek(Lives);
        rig.Players.Lives[1] = machine.Peek(Lives + 1);
        rig.Players.ExtendLetters[0] = first;
        rig.Players.ExtendLetters[1] = second;
        rig.End.Begin();

        machine.Poke(Letters, first);
        machine.Poke(Letters + 1, second);
        machine.Poke(Complete, 0);

        new ExtendBonus(rig.Players, rig.End).Update();

        machine.RunTo(0x0A5A, 20_000_000);

        Assert.Equal(machine.Peek(Lives), rig.Players.Lives[0]);
        Assert.Equal(machine.Peek(Lives + 1), rig.Players.Lives[1]);
        Assert.Equal(machine.Peek(Letters), rig.Players.ExtendLetters[0]);
        Assert.Equal(machine.Peek(Letters + 1), rig.Players.ExtendLetters[1]);
        Assert.Equal(machine.Peek(Complete), rig.End.Complete);
    }
}
