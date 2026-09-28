// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// $15C9-$15E0 and $3971, worked by hand off the reference.
public sealed class LevelFlowTests
{
    // $3971-$3975: $1C and $1E are the level's colour byte, high nibble first.
    [Fact]
    public void ALevelBeginsInItsOwnColours()
    {
        LevelFlow flow = new(new()) { Freeze = 9 };

        flow.Begin(0xB2);

        Assert.Equal(0x0B, flow.BackgroundOne);
        Assert.Equal(0x02, flow.BackgroundTwo);
        Assert.Equal(0xB2, flow.Colours);
        Assert.Equal(0, flow.Freeze);
    }

    // $15C9-$15CB: with $67 zero the pass goes straight on to the clock's tests.
    [Fact]
    public void WithoutAFreezeThePassCarriesOn()
    {
        LevelFlow flow = new(new());
        flow.Begin(0xB2);

        Assert.True(flow.Update());

        Assert.Equal(0, flow.Freeze);
        Assert.Equal(0xB2, flow.Colours);
    }

    // $15CC-$15CE: while $67 is above zero after its count, the rest of $1578 is skipped.
    [Fact]
    public void AFreezeCountsDownAndHoldsThePassBack()
    {
        LevelTimer timer = new();
        LevelFlow flow = new(timer);
        flow.Begin(0xB2);
        flow.Freeze = 3;
        timer.Frames = LevelTimer.Stopped;

        Assert.False(flow.Update());
        Assert.False(flow.Update());
        Assert.Equal(1, flow.Freeze);
        Assert.Equal(LevelTimer.Stopped, timer.Frames);
        Assert.Equal(0xB2, flow.Colours);
    }

    // $15CF-$15E0: the pass it reaches zero swaps the colours (EOR 7) and starts the clock at $32.
    [Fact]
    public void TheFreezeEndsBySwappingTheColoursAndStartingTheClock()
    {
        LevelTimer timer = new();
        LevelFlow flow = new(timer);
        flow.Begin(0xB2);
        flow.Freeze = 1;
        timer.Frames = LevelTimer.Stopped;

        Assert.True(flow.Update());

        Assert.Equal(0, flow.Freeze);
        Assert.Equal(0x0C, flow.BackgroundOne);
        Assert.Equal(0x05, flow.BackgroundTwo);
        Assert.Equal(LevelTimer.FramesPerSecond, timer.Frames);
    }

    // $2F68-$2F71: the swap that starts a freeze is the one that ends it, so two undo each other.
    [Fact]
    public void SwappingTwiceGivesTheColoursBack()
    {
        LevelFlow flow = new(new());
        flow.Begin(0xB2);

        flow.SwapColours();
        flow.SwapColours();

        Assert.Equal(0xB2, flow.Colours);
    }
}
