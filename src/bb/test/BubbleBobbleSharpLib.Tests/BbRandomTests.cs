// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests;

// $E9EA, worked by hand. The timer byte is held fixed so that what is left is the reference's own
// shift and fold.
public sealed class BbRandomTests
{
    // $81/$40 in: $27 takes $26's top bit and becomes $81, $26 takes $27's and becomes $02. $81 eor
    // $02 is $83, plus $81 is $104, plus the carry is $105, and the byte is $05.
    //
    // The carry is the one that separates a right reading from a wrong one. It is $26's top bit on
    // the way in, which `rol RESHO` leaves behind, and a translation that drops it returns $04.
    [Fact]
    public void ShiftsFoldsAndAddsTheCarryTheRotateLeft()
    {
        BbRandom random = Random(0x00);
        random.Low = 0x81;
        random.High = 0x40;

        Assert.Equal(0x05, random.Next());
        Assert.Equal(0x05, random.Low);
        Assert.Equal(0x81, random.High);
    }

    // $E9FB's `eor CIA1_TBLO`, last of all, and the result is what $26 keeps.
    [Fact]
    public void MixesInTheTimerByteLast()
    {
        BbRandom random = Random(0xFF);
        random.Low = 0x81;
        random.High = 0x40;

        Assert.Equal(0xFA, random.Next());
        Assert.Equal(0xFA, random.Low);
    }

    // Zero state and a zero timer stay at zero, which is what lets the loop's tests force a branch.
    [Fact]
    public void StaysAtZeroFromZero()
    {
        BbRandom random = Random(0x00);

        Assert.Equal(0x00, random.Next());
        Assert.Equal(0x00, random.Next());
    }

    private static BbRandom Random(int timer) => new(new FakeRandomSource { RandomValue = timer });
}
