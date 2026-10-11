// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

// A routine of the game's that waits in itself, a frame at a time, while the IRQ goes on. The pass that called it goes on
// once it has returned.
internal interface IGameHold
{
    // `$37` is up for the wait: the IRQ keeps the clock and moves nothing.
    public bool Paused { get; }

    // The colour the routine has put in the border and the background, or none while they are the level's black.
    public int? Border { get; }

    // One frame of the routine's wait. Returns false, without waiting, once there is no more.
    public bool Frame();

    // The routine's work between two passes' worth of frames.
    public void Pass();

    // The routine's work after its last wait. Takes and returns `$08`, the IRQ's frame counter.
    public byte Finish(byte counter);
}
