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
// What is left here is what the 6502 really does keep two of. The state, X and Y a player used to
// have here have gone to EntityTable: $B2, $BA and $C2 are eight-byte zero-page arrays that the six
// enemies share with the two players, and $105B reads the enemies' half of them as $B4, $BC and $C4.
// Keeping two of them here was a guess, and the reference has now contradicted it.
internal sealed class PlayerTable
{
    // Two, and the game has no way to say otherwise: the C64 conversion has two joystick ports.
    internal const int Capacity = 2;

    private readonly byte[] _lives = new byte[Capacity];
    private readonly byte[] _reload = new byte[Capacity];
    private readonly byte[] _blowFacing = new byte[Capacity];
    private readonly byte[] _extendLetters = new byte[Capacity];
    private readonly byte[] _blowReload = [0x08, 0x08];
    private readonly byte[] _blowState = [0x88, 0x88];
    private readonly byte[] _blowVariant = [0x04, 0x04];
    private readonly byte[] _blowType = [0x04, 0x04];
    private readonly byte[] _driftRing = new byte[Capacity];
    private readonly byte[] _walkRing = new byte[Capacity];
    private readonly byte[] _blowRing = new byte[Capacity];

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

    // $54 and $55. The EXTEND letters the player holds, one bit each from $AB53: $3DF9 sets them as
    // a letter bubble pops. extend-bonus.s reads them, and nothing translated does yet.
    internal Span<byte> ExtendLetters => _extendLetters;

    // $A77B, $A77D, $A77F and $A781: what a blow makes. $2364 copies the first into Reload and the
    // other three into the new bubble's $A9B2, $AA30 and $AA42. $7F53 sets them to these values
    // whenever a player starts or dies, and the candies at $2DAB, $2DB2 and $2DCD change them.
    internal Span<byte> BlowReload => _blowReload;

    internal Span<byte> BlowState => _blowState;

    internal Span<byte> BlowVariant => _blowVariant;

    internal Span<byte> BlowType => _blowType;

    // $61, $63 and $65. The three rings: while one is not zero, $2483 (a step of the drift), $22B4
    // (a step of the walk) and $23D7 (a blow) each call $7C21 and score ten points. $2F5F, $2F62
    // and $2F65 set them by decrementing them from zero, and $7F53 clears them. Nothing translated
    // reads them yet.
    internal Span<byte> DriftRing => _driftRing;

    internal Span<byte> WalkRing => _walkRing;

    internal Span<byte> BlowRing => _blowRing;
}
