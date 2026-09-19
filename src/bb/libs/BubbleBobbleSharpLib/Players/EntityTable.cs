// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Players;

// The arrays a player shares with every other moving thing, from the $85xx-$88xx region.
//
// They are not a separate table in the 6502 at all. sprites2-tables.s lays that region out as
// forty-byte rows - thirty-two bytes of level tiles, then eight bytes of something else - and these
// arrays are those eight-byte tails. That is why every one of them is forty apart: $8520, $8548,
// $8570 and the rest are the same eight slots seen row after row. SolidMap is the other half of the
// same region.
//
// Eight slots, because $1CBD starts its loop at seven and counts down to zero. Slots 0 and 1 are the
// two players, which is why a routine holding a player number can index these with it directly.
//
// One field per array, added as the routine that reads it arrives, the way PlayerTable is built.
// Section 6.4 asks for this shape and the movement code needs it: $220C and the movers index these
// with the same register they index the player's own bytes with.
internal sealed class EntityTable
{
    // $1CBD: ldx #$07, then dec and bpl. Eight slots, not the eighteen section 6.4 mentions - that
    // count belongs to PESSION at $CA, which is a different array holding entity types.
    internal const int Capacity = 8;

    private readonly byte[] _state = new byte[Capacity];
    private readonly byte[] _x = new byte[Capacity];
    private readonly byte[] _y = new byte[Capacity];
    private readonly byte[] _frame = new byte[Capacity];
    private readonly byte[] _animationTimer = new byte[Capacity];
    private readonly byte[] _bubbleTimer = new byte[Capacity];
    private readonly byte[] _riseCounter = new byte[Capacity];
    private readonly byte[] _fallCounter = new byte[Capacity];
    private readonly byte[] _leftFlag = new byte[Capacity];
    private readonly byte[] _rightFlag = new byte[Capacity];
    private readonly byte[] _groundState = new byte[Capacity];
    private readonly byte[] _colour = new byte[Capacity];
    private readonly byte[] _mode = new byte[Capacity];
    private readonly byte[] _holdTimer = new byte[Capacity];
    private readonly byte[] _flashTimer = new byte[Capacity];

    // $B2, ENESSION. Zero means the slot is empty, which is what the update loop tests first.
    //
    // This and the two below are the odd ones out here: they are zero page, not $85xx, so they are
    // not a forty-byte row of the region the rest of this class comes from. They are eight slots
    // all the same, and $105B proves it - it walks y from five down to zero over $B4, $BC and $C4,
    // which are these three arrays two bytes along. Players are slots 0 and 1; the six enemies are
    // slots 2 to 7.
    internal Span<byte> State => _state;

    // $BA, FA. A pixel column, and the movement routines let it wrap - see section 6.1.
    internal Span<byte> X => _x;

    // $C2. A pixel row, wrapped by hand rather than by the byte: the vertical mover turns $F5 into
    // $15 and $14 into $F5, so a player who leaves the top of the playfield comes back at the bottom.
    internal Span<byte> Y => _y;

    // $8520. Which sprite the thing is drawn with, and with it which way it faces: the movers set 0
    // to face right and 4 to face left, and bit 0 alternates to make the walk cycle.
    internal Span<byte> Frame => _frame;

    // $8610. Counts frames between animation steps. The walk toggles on every fourth.
    internal Span<byte> AnimationTimer => _animationTimer;

    // $8818. Negative means the thing is not in a bubble. $05C5 sets every slot to $FF as a level
    // starts, and the movers test bit 7 of it before they will turn a player round.
    internal Span<byte> BubbleTimer => _bubbleTimer;

    // $87A0. How much of a jump's rise is left. It counts down to zero and the fall begins, and
    // $05C5 sets it to $FF as a level starts.
    internal Span<byte> RiseCounter => _riseCounter;

    // $87C8. How far into a fall the thing is. It counts up to $10 and the fall ends.
    internal Span<byte> FallCounter => _fallCounter;

    // $8840 and $8868. Which way a thing drifts while it is off the ground, one flag each, negative
    // for on. $222B latches them as a jump starts and $2483 is the only thing that reads them. That
    // settles the drift, not the whole of airborne movement: $25F1 reads the stick again on top of
    // it - see PlayerSteer.
    internal Span<byte> LeftFlag => _leftFlag;

    internal Span<byte> RightFlag => _rightFlag;

    // $8548. The colour the thing's sprite is drawn in, and the only one of a multicolour sprite's
    // three colours that is the sprite's own - $D025 and $D026 hold the other two for every sprite
    // on the screen at once. $1822 reads it as it fills the VIC's colour registers, and $05C5 gives
    // the two players theirs as a level starts.
    internal Span<byte> Colour => _colour;

    // $8728. bubbles-sprites.s reads it as the flash a thing does after it is hurt, and $13FA sets
    // it to $FF as a captured enemy is let back out of its bubble.
    internal Span<byte> FlashTimer => _flashTimer;

    // $85C0. Which of two arms $1CDB takes for this slot: negative takes the ordinary per-slot
    // update and positive takes the other one. sprites2-tables.s starts the two players at $FF and
    // the six enemies at zero, and $10B9 puts a captured enemy back to $FF. bubbles-sprites.s counts
    // it down. Nothing translated so far reads it - it is here because the capture writes it.
    internal Span<byte> Mode => _mode;

    // $8638. A countdown that holds something off while it is not zero: $1D11 reads it, skips the
    // call at $1D21 and decrements it. The capture clears it. What it holds off is not translated
    // yet, so it is named for what it does rather than for what it stops.
    internal Span<byte> HoldTimer => _holdTimer;

    // $87F0. Negative while the thing has ground under it. $1F03 tests bit 7 before it will let a
    // player walk, and both $1F17 and $2575 start a fall by incrementing it out of $FF. $05C5 sets
    // it to $FF as a level starts.
    internal Span<byte> GroundState => _groundState;
}
