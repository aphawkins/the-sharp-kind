// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

// `$2E0B`-`$2E5E`, the special item of type 8's wipe: from the bottom row of the level to the top, a row every two frames,
// with the pause flag down so the IRQ moves everything on and `$045C` looks at the players before each row. In a row's
// 28 cells inside the walls, everything but `$10`-`$1E` (the level's tile) becomes `$FF`, a character the effect has made a
// solid block (`$47F8`, `$4FF8`), and the colour RAM becomes `$0B`. The level stays so until the next. Then a player the
// water carries (state `$0D`) is put on its feet, and the rest is the caller's (`$2E61`).
internal sealed class ScreenWipe(Action finish) : IGameHold
{
    internal const int Rows = 25;

    private const int FramesPerRow = 2;
    private const int Frames = Rows * FramesPerRow;

    private int _frames = Frames;

    public bool Paused => false;

    public int? Border => null;

    // How many rows, from the bottom, are wiped.
    internal int Wiped { get; private set; }

    // Whether the wipe is still going up the screen, which nothing else is drawn on until it is over.
    internal bool Wiping => _frames < Frames;

    public bool Frame()
    {
        if (_frames >= Frames)
        {
            return false;
        }

        _frames++;
        return true;
    }

    public void Pass() => Wiped = _frames / FramesPerRow;

    public byte Finish(byte counter)
    {
        finish();
        return counter;
    }

    // `$2DFE`: the wipe starts.
    internal void Begin()
    {
        _frames = 0;
        Wiped = 0;
    }

    // A level's start.
    internal void Reset()
    {
        _frames = Frames;
        Wiped = 0;
    }
}
