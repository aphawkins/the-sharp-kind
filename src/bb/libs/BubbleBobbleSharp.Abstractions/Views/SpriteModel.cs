// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The eight hardware sprites: the players and the enemies.</summary>
public sealed class SpriteModel
{
    private const int FirstPointer = 0x60;

    private const int SheetSprites = 97;

    private const byte FlashStates = 0x11;

    private static readonly byte[] s_flashColour = [0x05, 0x03, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A];

    private readonly bool[] _drawn;
    private readonly byte[] _x;
    private readonly byte[] _y;
    private readonly int[] _sprite;
    private readonly int[] _colour;

    /// <summary>Initializes a new instance of the <see cref="SpriteModel"/> class.</summary>
    /// <param name="state">Each slot's state byte.</param>
    /// <param name="x">Each slot's X position.</param>
    /// <param name="y">Each slot's Y position.</param>
    /// <param name="frame">Each slot's animation frame.</param>
    /// <param name="spriteBase">Each slot's sprite base.</param>
    /// <param name="colour">Each slot's sprite colour.</param>
    /// <param name="flashTimer">Each slot's flash timer.</param>
    /// <param name="enabled">The sprite enable bits, one a slot.</param>
    public SpriteModel(
        ReadOnlySpan<byte> state,
        ReadOnlySpan<byte> x,
        ReadOnlySpan<byte> y,
        ReadOnlySpan<byte> frame,
        ReadOnlySpan<byte> spriteBase,
        ReadOnlySpan<byte> colour,
        ReadOnlySpan<byte> flashTimer,
        byte enabled)
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
            int sprite = (byte)((frame[slot] & FrameMask) + spriteBase[slot]) - FirstPointer;

            _drawn[slot] = (enabled & (1 << slot)) != 0 && sprite is >= 0 and < SheetSprites;
            _sprite[slot] = sprite;

            bool flashing = state[slot] is not 0 and < FlashStates && flashTimer[slot] != 0;
            _colour[slot] = (flashing ? s_flashColour[slot] : colour[slot]) & ColourMask;
        }
    }

    /// <summary>Gets how many hardware sprites there are.</summary>
    public static int Capacity => 8;

    /// <summary>Gets the mask on a frame before it becomes a sprite pointer.</summary>
    public static int FrameMask => 0x1F;

    /// <summary>Gets the mask on a sprite's colour.</summary>
    public static int ColourMask => 0x0F;

    /// <summary>Whether a slot is drawn at all.</summary>
    /// <param name="slot">The slot, 0 to 7.</param>
    /// <returns>Whether that slot is drawn.</returns>
    public bool IsDrawn(int slot) => _drawn[slot];

    /// <summary>A slot's X position.</summary>
    /// <param name="slot">The slot, 0 to 7.</param>
    /// <returns>The position byte.</returns>
    public byte X(int slot) => _x[slot];

    /// <summary>A slot's Y position.</summary>
    /// <param name="slot">The slot, 0 to 7.</param>
    /// <returns>The position byte.</returns>
    public byte Y(int slot) => _y[slot];

    /// <summary>Which of the game's sprites a slot is drawn as.</summary>
    /// <param name="slot">The slot, 0 to 7.</param>
    /// <returns>The sprite's index in the sheet.</returns>
    public int Sprite(int slot) => _sprite[slot];

    /// <summary>The colour a slot is drawn in.</summary>
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
