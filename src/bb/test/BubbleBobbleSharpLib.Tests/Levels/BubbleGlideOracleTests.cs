// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The players' bubbles gliding to the next level's start points (`draw_animated_sprite`, $E554) against the real game run on
// SharpKind.Emulation. Player 1 is put somewhere on level 1 and the level's enemies taken away ($B4-$B9 and $4A written: an
// intervention); the game clears the level, waits its nine seconds, scrolls, and glides. Each time $E4DF puts the sprites
// where the player is, the place is read, and the port's line must go through the same places in the same order.
[Trait("Level", "Integration")]
public sealed class BubbleGlideOracleTests
{
    private const int Glide = 0xE554;
    private const int ShowPlace = 0xE4DF;
    private const int GlideReturn = 0x09EB;
    private const int PlayerX = 0xBA;
    private const int PlayerY = 0xC2;
    private const byte StartX = 0x2C;
    private const byte StartY = 0xDD;

    [Theory]
    [InlineData(0x66, 0xDD)]
    [InlineData(0x90, 0x60)]
    [InlineData(0x30, 0x40)]
    [InlineData(0xC8, 0xB5)]
    public void GlidesThroughTheGamesPlaces(byte x, byte y)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(20);
        machine.RunTo(Rebb64Machine.Pass, 50_000_000);

        for (int slot = 2; slot < 8; slot++)
        {
            machine.Poke(0xB2 + slot, 0);
        }

        machine.Poke(0x4A, 0);
        machine.RunTo(Glide, 985_248L * 30);

        // Where the player is when the glide starts, put there as it does (an intervention).
        machine.Poke(PlayerX, x);
        machine.Poke(PlayerY, y);

        List<(byte X, byte Y)> places = [];
        long limit = machine.Cycles + (985_248L * 30);

        // Every call of $E4DF in the glide shows a point; the glide is over when it returns.
        while (machine.Cpu.PC != GlideReturn && machine.Cycles < limit)
        {
            if (machine.Cpu.PC == ShowPlace)
            {
                places.Add((machine.Peek(PlayerX), machine.Peek(PlayerY)));
            }

            machine.Step();
        }

        Assert.Equal(GlideReturn, machine.Cpu.PC);

        int length = Math.Max(Math.Abs(x - StartX), Math.Abs(y - StartY));

        Assert.Equal(length, places.Count);

        for (int step = 1; step <= length; step++)
        {
            Assert.Equal(places[step - 1], BubbleGlide.Point(x, y, StartX, StartY, step));
        }
    }
}
