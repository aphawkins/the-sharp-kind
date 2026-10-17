// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Bubbles;

// Where a bubble comes to rest under level 1's ceiling, against the real game run on SharpKind.Emulation.
//
// No intervention: the game is played from the title, fire is held for five passes to blow one bubble, and slot 17's Y
// (`$AA1E`) is read at the start of every pass, up to the wobble that comes before the pop. The capture's summary said the bubble parked at `$47`; read a pass at a
// time it goes up and down between `$47` and `$45`, as the port's `BubbleTravelTests` has it.
[Trait("Level", "Integration")]
public sealed class BubbleCeilingOracleTests
{
    private const int Slot = 17;
    private const int Type = 0x00CA;
    private const int Y = 0xAA1E;

    private const int FireDown = 3;
    private const int FireUp = 8;
    private const int Passes = 112;

    private const byte High = 0x47;
    private const byte Low = 0x45;
    private const int Settled = 100;

    [Fact]
    public void TheBubbleRocksBetweenTheTwoRowsUnderTheCeiling()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        List<byte> path = [];

        for (int pass = 0; pass < Passes; pass++)
        {
            machine.Joystick = pass is >= FireDown and < FireUp ? (byte)0xEF : (byte)0xFF;
            machine.RunTo(Rebb64Machine.Pass, 985_248L * 5);

            // Off the entry, or the next RunTo stops where it is.
            machine.Step();
            path.Add(machine.Peek(Type + Slot) == 0 ? (byte)0 : machine.Peek(Y + Slot));
        }

        int first = path.IndexOf(Low);

        Assert.True(first >= 0, "the bubble never reached the ceiling");
        Assert.True(first < Settled, $"the bubble reached the ceiling late, at pass {first}");

        for (int pass = first + 1; pass < Passes; pass++)
        {
            Assert.Equal(pass % 2 == first % 2 ? Low : High, path[pass]);
        }
    }
}
