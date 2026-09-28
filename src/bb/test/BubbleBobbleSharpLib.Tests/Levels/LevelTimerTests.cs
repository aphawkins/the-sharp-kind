// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// $392A, $06AB and $0504, worked by hand off the reference.
public sealed class LevelTimerTests
{
    // $392A-$393C: thirty seconds, but ten on SUBFLG $37 and twenty from $38.
    [Theory]
    [InlineData(0x00, 0x1E)]
    [InlineData(0x36, 0x1E)]
    [InlineData(0x37, 0x0A)]
    [InlineData(0x38, 0x14)]
    [InlineData(0x63, 0x14)]
    public void ALevelBeginsWithItsTime(byte subflg, byte seconds)
    {
        LevelTimer timer = new();

        timer.Begin(subflg);

        Assert.Equal(seconds, timer.Seconds);
        Assert.Equal(seconds, timer.Start);
        Assert.Equal(0x32, timer.Frames);
        Assert.Equal(0, timer.Hurry);
    }

    // $06B2-$06C6: fifty frames make a second.
    [Fact]
    public void FiftyFramesMakeASecond()
    {
        LevelTimer timer = Begun();

        for (int frame = 1; frame < 50; frame++)
        {
            Assert.False(timer.Advance());
        }

        Assert.Equal(1, timer.Frames);
        Assert.Equal(0x1E, timer.Seconds);
        Assert.True(timer.Advance());
        Assert.Equal(0x32, timer.Frames);
        Assert.Equal(0x1D, timer.Seconds);
    }

    // $06AF: with $2B at $FF the clock does nothing.
    [Fact]
    public void AStoppedClockDoesNotRun()
    {
        LevelTimer timer = Begun();
        timer.Frames = LevelTimer.Stopped;
        timer.Ready = 2;

        for (int frame = 0; frame < 200; frame++)
        {
            Assert.False(timer.Advance());
        }

        Assert.Equal(LevelTimer.Stopped, timer.Frames);
        Assert.Equal(0x1E, timer.Seconds);
        Assert.Equal(2, timer.Ready);
    }

    // $06BB-$06C3: the READY!! seconds count down while they are above zero, and no further.
    [Theory]
    [InlineData(2, 1)]
    [InlineData(1, 0)]
    [InlineData(0, 0)]
    [InlineData(0xFF, 0xFF)]
    public void TheReadyTextCountsDownToZero(byte before, byte after)
    {
        LevelTimer timer = Begun();
        timer.Frames = 1;
        timer.Ready = before;

        Assert.True(timer.Advance());

        Assert.Equal(after, timer.Ready);
    }

    // $06B5: $2A is a byte, and goes on below zero.
    [Fact]
    public void TheSecondsWrapBelowZero()
    {
        LevelTimer timer = Begun();
        timer.Seconds = 0;
        timer.Frames = 1;

        timer.Advance();

        Assert.Equal(0xFF, timer.Seconds);
    }

    // $0504-$0512: the level's time again, and the hurry-up not begun.
    [Fact]
    public void ARestartGivesTheLevelItsTimeAgain()
    {
        LevelTimer timer = Begun();
        timer.Seconds = 3;
        timer.Hurry = 1;

        timer.Restart();

        Assert.Equal(0x1E, timer.Seconds);
        Assert.Equal(0, timer.Hurry);
    }

    // $0506, $050A: a stopped clock, or a cleared level, is left as it is.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ARestartLeavesAStoppedClockOrAClearedLevelAlone(bool stopped, bool cleared)
    {
        LevelTimer timer = Begun();
        timer.Seconds = 3;
        timer.Hurry = cleared ? (byte)0xFF : (byte)1;
        timer.Frames = stopped ? LevelTimer.Stopped : (byte)0x20;

        timer.Restart();

        Assert.Equal(3, timer.Seconds);
        Assert.Equal(cleared ? 0xFF : 1, timer.Hurry);
    }

    private static LevelTimer Begun()
    {
        LevelTimer timer = new();
        timer.Begin(0);
        return timer;
    }
}
