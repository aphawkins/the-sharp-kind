// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

// The level's clock: the seconds left in $2A, and the IRQ frames to the next second in $2B.
//
// $2C holds the value the level began with, for a restart, and $2D the hurry-up state (0 when it has not begun,
// and $FF once the level is cleared). $A9B1 is the seconds the ROUND and READY!! text has left, $FF when it is gone.
// The clock is the IRQ's ($06AB-$06C9): one call of Advance is one 50 Hz frame, and a pass of the game is more
// than one. $2B at $FF stops the clock, and the item timers ($5D, $5E) with it.
internal sealed class LevelTimer
{
    internal const byte FramesPerSecond = 0x32;

    internal const byte Stopped = 0xFF;

    private const byte DefaultSeconds = 0x1E;
    private const byte SlowSubflg = 0x37;
    private const byte SlowSeconds = 0x0A;
    private const byte FastFromSubflg = 0x38;
    private const byte FastSeconds = 0x14;

    internal byte Seconds { get; set; }

    internal byte Frames { get; set; } = Stopped;

    internal byte Start { get; private set; }

    internal byte Hurry { get; set; }

    internal byte Ready { get; set; } = Stopped;

    internal bool IsStopped => (sbyte)Frames < 0;

    // $392A-$393C for the time, $09FD for the frames, and $E07E for the hurry-up. The READY!! text
    // ($A9B1) is set by the round intro, which is step 4d's.
    internal void Begin(byte subflg)
    {
        Seconds = subflg switch
        {
            SlowSubflg => SlowSeconds,
            >= FastFromSubflg => FastSeconds,
            _ => DefaultSeconds,
        };

        Start = Seconds;
        Frames = FramesPerSecond;
        Hurry = 0;
    }

    // $06AB-$06C6: one IRQ frame. Returns whether a second went by, when $06C6 counts down the item timers too.
    internal bool Advance()
    {
        if (IsStopped || --Frames != 0)
        {
            return false;
        }

        Frames = FramesPerSecond;
        Seconds--;

        if ((sbyte)Ready > 0)
        {
            Ready--;
        }

        return true;
    }

    // $0504-$0512: a player coming back, or leaving, starts the level's time again unless the clock is stopped
    // or the level is cleared.
    internal void Restart()
    {
        if (IsStopped || (sbyte)Hurry < 0)
        {
            return;
        }

        Hurry = 0;
        Seconds = Start;
    }
}
