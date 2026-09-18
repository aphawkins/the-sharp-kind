// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>
/// Where the two players are and which of their images is showing - what
/// update_sprite_positions at $1805 leaves in the VIC-II's registers.
/// <para>
/// That routine is the whole of the drawing side, and it is short: for each
/// slot it writes $BA to the sprite's X register and $C2 to its Y, the colour
/// out of $8548 to the sprite's colour register, and $8520 masked to five bits
/// and added to $8598 to the sprite pointer. There is no other arithmetic
/// between a player's bytes and the screen, which is why this model carries
/// the position bytes as they stand: the offset from a sprite register to a
/// pixel is the VIC-II's own, and so belongs to a rendition rather than here.
/// </para>
/// <para>
/// $8598 is the same for both players - $60, which is the sprite pointer the
/// game's own sprite data starts at - so the pointer a player is drawn with is
/// the masked frame and nothing else. That is why <see cref="Sprite"/> is the
/// index into the sheet directly.
/// </para>
/// </summary>
public sealed class PlayerModel
{
    private readonly bool[] _active;
    private readonly byte[] _x;
    private readonly byte[] _y;
    private readonly int[] _sprite;
    private readonly int[] _colour;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerModel"/> class.
    /// </summary>
    /// <param name="state">Each player's state byte, as $B2 holds it.</param>
    /// <param name="x">Each player's X position, as $BA holds it.</param>
    /// <param name="y">Each player's Y position, as $C2 holds it.</param>
    /// <param name="frame">Each player's animation frame, as $8520 holds it.</param>
    /// <param name="colour">Each player's sprite colour, as $8548 holds it.</param>
    public PlayerModel(
        ReadOnlySpan<byte> state,
        ReadOnlySpan<byte> x,
        ReadOnlySpan<byte> y,
        ReadOnlySpan<byte> frame,
        ReadOnlySpan<byte> colour)
    {
        Check(state, nameof(state));
        Check(x, nameof(x));
        Check(y, nameof(y));
        Check(frame, nameof(frame));
        Check(colour, nameof(colour));

        _active = new bool[Capacity];
        _x = x[..Capacity].ToArray();
        _y = y[..Capacity].ToArray();
        _sprite = new int[Capacity];
        _colour = new int[Capacity];

        for (int player = 0; player < Capacity; player++)
        {
            // $1817. A slot whose state byte is zero is empty, and the routine skips the colour
            // work for it - but it writes its position and its pointer regardless, because the VIC
            // draws whatever its registers hold. What keeps an empty slot off the screen is
            // $D015, the sprite enable register, which this port reads as the slot being active.
            _active[player] = state[player] != 0;
            _sprite[player] = frame[player] & FrameMask;
            _colour[player] = colour[player] & ColourMask;
        }
    }

    /// <summary>
    /// Gets how many players there are. Two, for the C64's two joystick ports -
    /// the same count <c>PlayerTable</c> is built to.
    /// </summary>
    public static int Capacity => 2;

    /// <summary>
    /// Gets the mask $182F puts on a frame before it becomes a sprite pointer:
    /// thirty-two images, which is the block of sprite data the players, the
    /// bubbles and the EXTEND letters share.
    /// </summary>
    public static int FrameMask => 0x1F;

    /// <summary>
    /// Gets the mask on a sprite's colour. The VIC-II's sprite colour register
    /// holds four bits, so a colour byte above fifteen names the colour its low
    /// nibble does.
    /// </summary>
    public static int ColourMask => 0x0F;

    /// <summary>
    /// Whether a player is on the screen at all.
    /// </summary>
    /// <param name="player">The player, 0 or 1.</param>
    /// <returns>Whether that player is drawn.</returns>
    public bool IsActive(int player) => _active[player];

    /// <summary>
    /// A player's X position, as $BA holds it and as $1809 writes it to the
    /// sprite's own X register.
    /// </summary>
    /// <param name="player">The player, 0 or 1.</param>
    /// <returns>The position byte.</returns>
    public byte X(int player) => _x[player];

    /// <summary>
    /// A player's Y position, as $C2 holds it and as $180E writes it to the
    /// sprite's own Y register.
    /// </summary>
    /// <param name="player">The player, 0 or 1.</param>
    /// <returns>The position byte.</returns>
    public byte Y(int player) => _y[player];

    /// <summary>
    /// Which of the game's sprites a player is drawn as, counted from the first
    /// one in the sheet.
    /// <para>
    /// $182F masks the frame and stores it back to $8520 before adding the base
    /// to it. The store is not reproduced, because nothing translated so far
    /// leaves a frame above $1F there for it to trim - the movers set frames 0
    /// to 7 - and a model does not write to the game's state. It is a gap, and
    /// it closes when a routine that can overflow a frame arrives.
    /// </para>
    /// </summary>
    /// <param name="player">The player, 0 or 1.</param>
    /// <returns>The sprite's index in the sheet.</returns>
    public int Sprite(int player) => _sprite[player];

    /// <summary>
    /// The colour a player is drawn in, as $8548 holds it: one of the sixteen,
    /// and the only one of a sprite's three that is the sprite's own.
    /// <para>
    /// $1822 can override it while a player is flashing, from $8570. For the
    /// two players those two arrays hold the same pair of colours - 5 and 3 -
    /// so the override changes nothing, and the flash is left to the phase that
    /// brings invincibility over.
    /// </para>
    /// </summary>
    /// <param name="player">The player, 0 or 1.</param>
    /// <returns>The colour's entry number.</returns>
    public int Colour(int player) => _colour[player];

    private static void Check(ReadOnlySpan<byte> bytes, string name)
    {
        if (bytes.Length < Capacity)
        {
            throw new ArgumentException(
                $"There are {Capacity} players, and only {bytes.Length} bytes of '{name}'.",
                name);
        }
    }
}
