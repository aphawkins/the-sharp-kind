// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Players;

// The two players' bytes, kept the way the 6502 keeps them: one array per field, indexed by player
// rather than one object per player. That is the parallel-array shape bb-port-plan.md section 6.4
// asks for, and it is load-bearing while translating - every routine Phase 4 brings over indexes
// these by the player number it is already holding.
//
// master.s gives the addresses. Three of the fields are zero-page arrays the 6502 indexes exactly as
// this class does, so player 0 is the byte at the base and player 1 the byte after it. The other two
// are not arrays there at all: master.s names $5D and $5E, and game-variables.s names $045A and
// $045B, as a pair of separate bytes each. They are indexed here anyway, because a player number is
// what the caller has, and a pair of named fields would make every caller branch on it.
internal sealed class PlayerTable
{
    // Two, and the game has no way to say otherwise: the C64 conversion has two joystick ports.
    internal const int Capacity = 2;

    private readonly byte[] _state = new byte[Capacity];
    private readonly byte[] _x = new byte[Capacity];
    private readonly byte[] _y = new byte[Capacity];
    private readonly byte[] _bubbleTimer = new byte[Capacity];
    private readonly byte[] _lives = new byte[Capacity];
    private readonly byte[] _reload = new byte[Capacity];
    private readonly byte[] _blowFacing = new byte[Capacity];

    // $B2, ENESSION. Zero means the slot is empty, which is what the update loop tests first.
    internal Span<byte> State => _state;

    // $BA, FA. A pixel column, and the movement routines let it wrap - see section 6.1.
    internal Span<byte> X => _x;

    // $C2. A pixel row, wrapped by hand rather than by the byte: the vertical mover turns $F5 into
    // $15 and $14 into $F5, so a player who leaves the top of the playfield comes back at the bottom.
    internal Span<byte> Y => _y;

    // $5D and $5E. bubbles-sprites.s reads these as the timer for the blowing animation drawn in
    // characters behind the player, which is a screen effect rather than a rule. An earlier note
    // here called it the reload; the reload is Reload below, and nothing reads these two yet.
    internal Span<byte> BubbleTimer => _bubbleTimer;

    // $045A and $045B, the two bytes game-variables.s sets aside for the lives counters.
    internal Span<byte> Lives => _lives;

    // $A824 and $A825. How many frames until the player may blow again. $2385 sets it to eight as a
    // bubble leaves and $0A28 counts it down once a frame, and $22E8 refuses to start a blow while
    // it is anything but zero. game-loop.s calls it an invincibility timer, which is the sixth
    // comment in the reference to point the wrong way: nothing reads it but the blow.
    //
    // Per player rather than per entity slot, because the 6502 has two bytes here and no more.
    internal Span<byte> Reload => _reload;

    // $ACCB and $ACCC. Which way the player faced as the blow started, kept for the six frames it
    // runs so that every frame of the animation is built from the same facing. $22E8 stores $00 or
    // $02 and $2301 doubles it back into a sprite frame.
    internal Span<byte> BlowFacing => _blowFacing;
}
