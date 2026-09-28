// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

// What happens to a level as it goes, in the second half of $1578 (from $15C9): the freeze, then the clock
// running out. Only the freeze is here so far.
//
// The freeze is $67. An item effect ($2F74, step 6) sets it to $87 and stops the clock, and swaps the
// level's two background colours ($1C, $1E: EOR 7). Each pass counts it down, and the pass it reaches zero
// swaps the colours back and starts the clock again. Until then the enemies keep still ($1CF5), and the pass
// stops short of the clock's tests.
internal sealed class LevelFlow
{
    private const byte ColourSwap = 0x07;

    private const int NibbleBits = 4;
    private const int NibbleMask = 0x0F;

    private readonly LevelTimer _timer;

    internal LevelFlow(LevelTimer timer)
    {
        ArgumentNullException.ThrowIfNull(timer);

        _timer = timer;
    }

    internal byte Freeze { get; set; }

    internal byte BackgroundOne { get; private set; }

    internal byte BackgroundTwo { get; private set; }

    // The level's colour byte as it stands: entry 01 the high nibble, entry 10 the low.
    internal int Colours => (BackgroundOne << NibbleBits) | BackgroundTwo;

    // $0633 clears the freeze, and $3971-$3975 copy the level's colours in.
    internal void Begin(int colours)
    {
        Freeze = 0;
        BackgroundOne = (byte)((colours >> NibbleBits) & NibbleMask);
        BackgroundTwo = (byte)(colours & NibbleMask);
    }

    // $2F68: the swap that starts a freeze, and ends it at $15CF.
    internal void SwapColours()
    {
        BackgroundOne ^= ColourSwap;
        BackgroundTwo ^= ColourSwap;
    }

    // $15C9-$15E0. Returns whether the rest of $1578 runs this pass.
    internal bool Update()
    {
        if (Freeze == 0)
        {
            return true;
        }

        if (--Freeze != 0)
        {
            return false;
        }

        SwapColours();
        _timer.Frames = LevelTimer.FramesPerSecond;

        return true;
    }
}
