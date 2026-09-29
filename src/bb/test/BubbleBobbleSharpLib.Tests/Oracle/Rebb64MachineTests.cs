// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using Xunit;

namespace BubbleBobbleSharpLib.Tests.Oracle;

// The oracle itself: the real game gets to a level from the title screen, headless.
public sealed class Rebb64MachineTests
{
    [Fact]
    public void TheGameStartsAPlayerAndReachesItsFirstPass()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();

        machine.StartOnePlayer();

        Assert.Equal(Rebb64Machine.Pass, machine.Cpu.PC);
        Assert.Equal(0x01, machine.Peek(0xB2));
        Assert.Equal(0x00, machine.Peek(0xB3));
    }

    [Fact]
    public void ARoutineCanBeBracketedWithNoInterruptInsideIt()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();

        // $0CF2, the object walk, is called from $0A4E every pass and returns to $0A51.
        Capture capture = machine.Bracket(0x0CF2, 0x0A51, 2_000_000);

        // The IRQ counts frames in $08, so it did not run inside the routine.
        Assert.Equal(capture.Before[0x08], capture.After[0x08]);
        Assert.Equal(0x0A51, machine.Cpu.PC);
        Assert.Equal(65536, capture.Before.Length);
    }
}
