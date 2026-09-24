// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>
/// The eight hardware sprites - the two players in slots 0 and 1, the enemies
/// in 2 to 7 - where they are and which image each shows: what
/// update_sprite_positions at $1805 leaves in the VIC-II's registers.
/// <para>
/// That routine is the whole of the drawing side, and it is short: for each
/// slot it writes $BA to the sprite's X register and $C2 to its Y, the colour
/// out of $8548 to the sprite's colour register, and $8520 masked to five bits
/// and added to $8598 to the sprite pointer. There is no other arithmetic
/// between a slot's bytes and the screen, which is why this model carries
/// the position bytes as they stand: the offset from a sprite register to a
/// pixel is the VIC-II's own, and so belongs to a rendition rather than here.
/// </para>
/// <para>
/// A pointer names 64 bytes of the VIC bank at $4000. The game's own sprites
/// start at $5800, pointer $60, and run to $C0, so <see cref="Sprite"/> counts
/// from there. $8598 is $60 for both players, and $73 to $BB for the enemy
/// classes, so every pointer the translated routines make lands in that run -
/// except food's.
/// </para>
/// </summary>
public sealed class SpriteModel
{
    // $5800 in the VIC bank at $4000, sixty-four bytes a sprite.
    private const int FirstPointer = 0x60;

    // $5800's 97 sprites.
    private const int SheetSprites = 97;

    // $1826. States from $11 up keep their own colour while flashing.
    private const byte FlashStates = 0x11;

    // $8570: $451E copies it from $47B5 once, at boot, and nothing writes it again.
    private static readonly byte[] s_flashColour = [0x05, 0x03, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A];

    private readonly bool[] _drawn;
    private readonly byte[] _x;
    private readonly byte[] _y;
    private readonly int[] _sprite;
    private readonly int[] _colour;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteModel"/> class.
    /// </summary>
    /// <param name="state">Each slot's state byte, as $B2 holds it.</param>
    /// <param name="x">Each slot's X position, as $BA holds it.</param>
    /// <param name="y">Each slot's Y position, as $C2 holds it.</param>
    /// <param name="frame">Each slot's animation frame, as $8520 holds it.</param>
    /// <param name="spriteBase">Each slot's sprite base, as $8598 holds it.</param>
    /// <param name="colour">Each slot's sprite colour, as $8548 holds it.</param>
    /// <param name="flashTimer">Each slot's flash timer, as $8728 holds it.</param>
    public SpriteModel(
        ReadOnlySpan<byte> state,
        ReadOnlySpan<byte> x,
        ReadOnlySpan<byte> y,
        ReadOnlySpan<byte> frame,
        ReadOnlySpan<byte> spriteBase,
        ReadOnlySpan<byte> colour,
        ReadOnlySpan<byte> flashTimer)
    {
        Check(state, nameof(state));
        Check(x, nameof(x));
        Check(y, nameof(y));
        Check(frame, nameof(frame));
        Check(spriteBase, nameof(spriteBase));
        Check(colour, nameof(colour));
        Check(flashTimer, nameof(flashTimer));

        _drawn = new bool[Capacity];
        _x = x[..Capacity].ToArray();
        _y = y[..Capacity].ToArray();
        _sprite = new int[Capacity];
        _colour = new int[Capacity];

        for (int slot = 0; slot < Capacity; slot++)
        {
            // $182F. The add is a byte add, so it wraps.
            int sprite = (byte)((frame[slot] & FrameMask) + spriteBase[slot]) - FirstPointer;

            // $1817. A slot whose state byte is zero is empty, and the routine skips the colour
            // work for it - but it writes its position and its pointer regardless, because the VIC
            // draws whatever its registers hold. What keeps an empty slot off the screen is
            // $D015, the sprite enable register, which this port reads as the slot being in use.
            //
            // A pointer outside $5800's sprites is not drawn either. The one the translated routines
            // make is food's: base $F2, $7C80 onwards, which $2921 builds at run time and which is
            // not translated. It is a gap, and closes with $2921.
            _drawn[slot] = state[slot] != 0 && sprite is >= 0 and < SheetSprites;
            _sprite[slot] = sprite;

            // $1822. A slot in play below state $11 with its flash timer running takes $8570's
            // colour instead - an angry enemy, let out of its bubble, is drawn in light red.
            bool flashing = state[slot] is not 0 and < FlashStates && flashTimer[slot] != 0;
            _colour[slot] = (flashing ? s_flashColour[slot] : colour[slot]) & ColourMask;
        }
    }

    /// <summary>
    /// Gets how many hardware sprites there are: the VIC-II's eight, which are
    /// <c>EntityTable</c>'s eight slots.
    /// </summary>
    public static int Capacity => 8;

    /// <summary>
    /// Gets the mask $182F puts on a frame before it becomes a sprite pointer:
    /// thirty-two images from the slot's base.
    /// </summary>
    public static int FrameMask => 0x1F;

    /// <summary>
    /// Gets the mask on a sprite's colour. The VIC-II's sprite colour register
    /// holds four bits, so a colour byte above fifteen names the colour its low
    /// nibble does.
    /// </summary>
    public static int ColourMask => 0x0F;

    /// <summary>
    /// Whether a slot is drawn at all.
    /// </summary>
    /// <param name="slot">The slot, 0 to 7.</param>
    /// <returns>Whether that slot is drawn.</returns>
    public bool IsDrawn(int slot) => _drawn[slot];

    /// <summary>
    /// A slot's X position, as $BA holds it and as $1809 writes it to the
    /// sprite's own X register.
    /// </summary>
    /// <param name="slot">The slot, 0 to 7.</param>
    /// <returns>The position byte.</returns>
    public byte X(int slot) => _x[slot];

    /// <summary>
    /// A slot's Y position, as $C2 holds it and as $180E writes it to the
    /// sprite's own Y register.
    /// </summary>
    /// <param name="slot">The slot, 0 to 7.</param>
    /// <returns>The position byte.</returns>
    public byte Y(int slot) => _y[slot];

    /// <summary>
    /// Which of the game's sprites a slot is drawn as, counted from the first
    /// one at $5800: the sprite pointer less $60.
    /// <para>
    /// $182F masks the frame and stores it back to $8520 before adding the base
    /// to it. The store is not reproduced here, because a model does not write
    /// to the game's state. Whether any translated routine leaves a frame above
    /// $1F for it to trim, now that the enemies pass through, is unchecked.
    /// </para>
    /// </summary>
    /// <param name="slot">The slot, 0 to 7.</param>
    /// <returns>The sprite's index in the sheet.</returns>
    public int Sprite(int slot) => _sprite[slot];

    /// <summary>
    /// The colour a slot is drawn in, as $8548 holds it: one of the sixteen,
    /// and the only one of a sprite's three that is the sprite's own.
    /// <para>
    /// $1822 overrides it while a slot is flashing, from $8570. For the two
    /// players that is the same pair of colours - 5 and 3 - so only an enemy
    /// changes: to 10, light red.
    /// </para>
    /// </summary>
    /// <param name="slot">The slot, 0 to 7.</param>
    /// <returns>The colour's entry number.</returns>
    public int Colour(int slot) => _colour[slot];

    private static void Check(ReadOnlySpan<byte> bytes, string name)
    {
        if (bytes.Length < Capacity)
        {
            throw new ArgumentException(
                $"There are {Capacity} sprites, and only {bytes.Length} bytes of '{name}'.",
                name);
        }
    }
}
