// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Bubbles;

// The eighteen-slot arrays, from the $A9xx and $AAxx region and three zero-page arrays beside them.
//
// This is the second of the game's two tables of moving things, and the distinction matters.
// EntityTable is the eight-slot region at $85xx to $88xx - the two players and the six enemies that
// walk about like them. This one is the eighteen slots $CA indexes: bubbles, items, and enemies once
// they are inside a bubble. $2321 searches it for somewhere to put a new bubble and $217C scans it
// for something to catch, and both count from seventeen down to zero.
//
// It is named for the bubble because the bubble is the only thing that fills it so far. Phase 6 puts
// enemies in the same slots and will want a wider name; it is left until there is something to name
// it after.
//
// One field per array, added as the routine that writes it arrives - the shape PlayerTable and
// EntityTable are built in. Every field here is written by the spawn at $232A.
internal sealed class ObjectTable
{
    // $2321: ldy #$11, then dey and bpl. Eighteen slots.
    internal const int Capacity = 18;

    // $0620. Every slot starts at $FF, which is what "free" means below: the search at $2321 takes
    // the first slot whose type byte is negative.
    internal const byte FreeType = 0xFF;

    private readonly byte[] _type = new byte[Capacity];
    private readonly byte[] _x = new byte[Capacity];
    private readonly byte[] _y = new byte[Capacity];
    private readonly byte[] _subX = new byte[Capacity];
    private readonly byte[] _subY = new byte[Capacity];
    private readonly byte[] _column = new byte[Capacity];
    private readonly byte[] _row = new byte[Capacity];
    private readonly byte[] _flags = new byte[Capacity];
    private readonly byte[] _behaviour = new byte[Capacity];
    private readonly byte[] _state = new byte[Capacity];
    private readonly byte[] _variant = new byte[Capacity];
    private readonly byte[] _enemyType = new byte[Capacity];
    private readonly byte[] _direction = new byte[Capacity];

    internal ObjectTable() => Reset();

    // $CA, PESSION. What is in the slot: $16 is a bubble, and $FF is nothing at all. The reference
    // heads it "enemy type array", which it is not - a bubble with no enemy in it lives here too.
    internal Span<byte> Type => _type;

    // $AA0C and $AA1E. A pixel column and a pixel row, in the same units the players' $BA and $C2
    // use, so that $217C can subtract one from the other without converting either.
    internal Span<byte> X => _x;

    internal Span<byte> Y => _y;

    // $A9C4 and $A9D6. How far past its cell the thing sits. Not the same scale in both axes: $2334
    // halves the horizontal one and $2350 leaves the vertical one alone, which is the reference's
    // own asymmetry rather than a slip here.
    internal Span<byte> SubX => _subX;

    internal Span<byte> SubY => _subY;

    // $DC and $EE. The cell the thing is in, kept beside the pixel position rather than worked out
    // from it. The 6502 stores both and later routines write them apart, so deriving one from the
    // other here would be a guess about code that has not been translated.
    internal Span<byte> Column => _column;

    internal Span<byte> Row => _row;

    // $A9FA and $A9E8. Both are set to $7D as a bubble is made, and neither is read by anything
    // Phase 5 has translated. The reference calls $A9E8 the AI routine index.
    internal Span<byte> Flags => _flags;

    internal Span<byte> Behaviour => _behaviour;

    // $A9B2, $AA30 and $AA42. The three bytes $2385 copies out of the per-player attribute tables,
    // which is all that is known about them so far.
    internal Span<byte> State => _state;

    internal Span<byte> Variant => _variant;

    internal Span<byte> EnemyType => _enemyType;

    // $0193. $00 or $80, and the only thing that sets it is which way the player faced as they blew.
    internal Span<byte> Direction => _direction;

    // $0620 and $062B, the part of $05C5 that reaches this table. The type bytes go to $FF and the
    // two cell arrays to zero; $062B's count of $24 covers both of them at once, because $DC and
    // $EE are eighteen bytes each and adjacent.
    internal void Reset()
    {
        _type.AsSpan().Fill(FreeType);
        _column.AsSpan().Clear();
        _row.AsSpan().Clear();
    }

    // $2321. From the last slot downwards, and the first free one wins. Returns -1 where the 6502
    // returns by falling out of the loop at $2329 with nothing done.
    internal int FindFree()
    {
        for (int slot = Capacity - 1; slot >= 0; slot--)
        {
            if ((_type[slot] & 0x80) != 0)
            {
                return slot;
            }
        }

        return -1;
    }
}
