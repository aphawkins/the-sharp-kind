// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The players' dragons in bubbles gliding to the new level's start points once the screen has scrolled to it
// (`draw_animated_sprite`, $E554-$E5BF, from $09E8): a Bresenham line ($E5C4) from where each player was to where the level
// starts it, a point a frame ($E4DF waits for each), both players at once, until the longer line is done. The split is still
// on ($20 is cleared at $E5BD), so the IRQ goes on turning the bubbles' frame every eight frames ($0746).
internal sealed class BubbleGlide
{
    private const int Players = 2;

    // A pass is two frames of the C64's.
    private const int PointsPerPass = 2;
    private const int FramesPerBubble = 8;

    private const byte FirstPointer = 0x60;

    // `$E52C`: the players' dragons in bubbles are green and blue.
    private static readonly byte[] s_colours = [0x05, 0x03, 0, 0, 0, 0, 0, 0];

    private readonly byte[] _x = new byte[Players];
    private readonly byte[] _y = new byte[Players];
    private readonly int _framesBefore;

    // Where the players are as the screen finishes scrolling, and how many frames the bubbles have turned for already.
    internal BubbleGlide(EntityTable entities, int framesBefore)
    {
        ArgumentNullException.ThrowIfNull(entities);

        for (int player = 0; player < Players; player++)
        {
            _x[player] = entities.Slot(player).X;
            _y[player] = entities.Slot(player).Y;
        }

        _framesBefore = framesBefore;
    }

    // How many points of the lines have been shown.
    internal int Points { get; private set; }

    // $E4DF: the players in the game, at their places, in their dragons in bubbles.
    internal static SpriteModel Bubbles(EntityTable entities, ReadOnlySpan<byte> x, ReadOnlySpan<byte> y, int frame)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return Bubbles((byte)((entities.Slot(0).State != 0 ? 1 : 0) | (entities.Slot(1).State != 0 ? 2 : 0)), x, y, frame);
    }

    // $E4DF: the players given, a bit each, at their places in their dragons in bubbles.
    internal static SpriteModel Bubbles(byte players, ReadOnlySpan<byte> x, ReadOnlySpan<byte> y, int frame)
    {
        byte[] none = new byte[SpriteModel.Capacity];
        byte[] pointers = new byte[SpriteModel.Capacity];

        Array.Fill(pointers, FirstPointer);

        return new(none, x, y, none, pointers, s_colours, none, players) { TransitFrame = frame };
    }

    // $0746: the IRQ turns the bubbles' frame every eight frames.
    internal static int BubbleFrame(int frames)
    {
        int turns = frames / FramesPerBubble;

        return turns % 2;
    }

    // $E5C4-$E657: the line's point after a number of steps, the start before the first and the end after the last. The
    // error starts at half the longer side; each step adds the shorter, and past the longer it takes the longer off and steps
    // along the shorter side too.
    internal static (byte X, byte Y) Point(byte fromX, byte fromY, byte toX, byte toY, int steps)
    {
        int dx = Math.Abs(fromX - toX);
        int dy = Math.Abs(fromY - toY);
        int stepX = toX > fromX ? 1 : -1;
        int stepY = toY > fromY ? 1 : -1;
        bool steep = dy > dx;
        int major = steep ? dy : dx;
        int minor = steep ? dx : dy;
        int error = major / 2;
        int x = fromX;
        int y = fromY;

        for (int step = 0; step < Math.Min(steps, major); step++)
        {
            error += minor;

            bool both = error >= major;

            if (both)
            {
                error -= major;
            }

            x += steep && !both ? 0 : stepX;
            y += steep || both ? stepY : 0;
        }

        return ((byte)x, (byte)y);
    }

    // $E594: the glide is over when neither line has a point left. The level has started, so its start points are the
    // players' places.
    internal bool Finished(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        int length = 0;

        for (int player = 0; player < Players; player++)
        {
            if (entities.Slot(player).State != 0)
            {
                length = Math.Max(length, Length(_x[player], _y[player], entities.Slot(player).X, entities.Slot(player).Y));
            }
        }

        return Points >= length;
    }

    internal void Pass() => Points += PointsPerPass;

    internal SpriteModel Sprites(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        byte[] x = new byte[SpriteModel.Capacity];
        byte[] y = new byte[SpriteModel.Capacity];

        for (int player = 0; player < Players; player++)
        {
            (x[player], y[player]) = Point(_x[player], _y[player], entities.Slot(player).X, entities.Slot(player).Y, Points);
        }

        return Bubbles(entities, x, y, BubbleFrame(_framesBefore + Points));
    }

    // $E5C4: the line's length is its longer side, a point for each step along it.
    private static int Length(byte fromX, byte fromY, byte toX, byte toY)
        => Math.Max(Math.Abs(fromX - toX), Math.Abs(fromY - toY));
}
