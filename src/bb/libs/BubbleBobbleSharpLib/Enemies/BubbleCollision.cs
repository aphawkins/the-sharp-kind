// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $105B and $1090 in enemy-ai.s: whether the thing that is moving has just met an enemy, and what
// happens to the enemy when it has.
//
// The dispatcher calls this twice, once down each arm of $0F98, straight after the eight pixel step
// and before any terrain probe. That order matters: the enemy is tested against where the thing has
// already moved to, not against where it came from.
//
// It is the reason the three arrays moved to EntityTable. $105B walks $B4, $BC and $C4 with y from
// five down to zero, and those are State, X and Y two bytes along - so the six enemies are slots 2
// to 7 of the same eight the two players occupy.
internal sealed class BubbleCollision
{
    // $105B's `ldy #$05`, then down to zero. Six enemies.
    private const int EnemyCount = 6;

    // $B4 against $B2, $BC against $BA, $C4 against $C2. Two bytes along in all three, so an enemy's
    // y is slot y + 2 of the eight.
    private const int EnemyBase = 2;

    // $1077 and $1088. The same sixteen pixel box as the catch at $0D02 and the one at $101C, and
    // this one is square in both axes - no nudge either way.
    private const byte Range = 0x10;

    // $1060 and $1064. A slot in this band is passed over, and so is an empty one. Everything else
    // is tested, including anything at or above $16.
    private const byte SkipFrom = 0x0B;
    private const byte SkipBefore = 0x16;

    // $1093's `asl` then `adc #$18`. The enemy's index doubled, so the six give $18 to $22 in twos.
    private const byte CapturedBase = 0x18;

    // $10BB. What the capture leaves in $A9FA.
    private const byte CapturedFlags = 0xA0;

    // $AB81 in game-tables-2.s, as the reference labels it: eight bytes, then $AB89 begins.
    //
    // **The index can run past it.** $10BF indexes this with the byte $10B1 has just written to
    // $AA30, which is either an enemy's rise counter - $FF at level start - or its state byte, which
    // this routine only reaches when it is below $0B. Nine is already past eight. The 6502 reads on
    // into $AB89 and keeps going, and there is nothing here that says where the table really stops.
    //
    // So this throws rather than guessing at bytes, the way $ACB6 does in EnemyDispatcher. A capture
    // from VICE is what settles the real bound, and until then a throw says plainly that the port
    // does not know. See docs/bb-port-plan.md.
    private static readonly byte[] s_scores = [0x02, 0x00, 0x0B, 0x0B, 0x0B, 0x07, 0x0B, 0x07];

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;

    internal BubbleCollision(ObjectTable objects, EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);

        _objects = objects;
        _entities = entities;
    }

    // $105B. The last enemy first, and the first one within range wins - $1090 ends in an rts, so
    // the walk stops there and the other enemies are never looked at.
    //
    // Returns true where the 6502 captured. The dispatcher does not read that directly: $1090 puts
    // a type of $18 or more in the slot, and $0FB4's `cmp #$06` then sends the move home before it
    // probes anything. The value is returned anyway because a caller that has to infer a capture
    // from a type byte is a caller waiting to get it wrong.
    internal bool Check(int slot)
    {
        for (int enemy = EnemyCount - 1; enemy >= 0; enemy--)
        {
            int entity = enemy + EnemyBase;
            byte state = _entities.State[entity];

            // $105D. Empty, or in the band $0B to $15, and this enemy is not there to be hit.
            if (state is 0 or (>= SkipFrom and < SkipBefore))
            {
                continue;
            }

            if (Distance.Absolute(_entities.X[entity], _objects.X[slot]) >= Range
                || Distance.Absolute(_entities.Y[entity], _objects.Y[slot]) >= Range)
            {
                continue;
            }

            Capture(slot, enemy, entity);
            return true;
        }

        return false;
    }

    // $1090. The enemy goes out of the eight and into the slot that hit it.
    private void Capture(int slot, int enemy, int entity)
    {
        // $1091. The enemy's own index doubled and offset, so which of the six it was survives in
        // the type byte rather than having to be looked up again.
        _objects.Type[slot] = (byte)((enemy << 1) + CapturedBase);

        byte state = _entities.State[entity];
        byte carried = state;

        // $1098. Only an enemy at $0A, or at $16 and above, has its three counters reset - and the
        // byte that goes on to $AA30 is then the rise counter it used to have rather than its state.
        //
        // That swap is easy to miss. `cmp` does not touch the accumulator, so on the short path the
        // `sta D_AA30,x` at $10B1 is still storing what `lda $B4,y` loaded at $1095. Two different
        // bytes reach the same place depending on a branch taken fifteen instructions earlier, and
        // the score below is indexed with whichever one it was.
        if (state is 0x0A or >= SkipBefore)
        {
            carried = _entities.RiseCounter[entity];

            _entities.RiseCounter[entity] = 0xFF;
            _entities.FallCounter[entity] = 0xFF;
            _entities.GroundState[entity] = 0xFF;
        }

        _objects.Variant[slot] = carried;

        // $10B4. The enemy is emptied out of the eight slots and the slot that caught it loses its
        // AI counter, so the dispatcher will not step it again.
        _entities.State[entity] = 0;
        _entities.HoldTimer[entity] = 0;
        _objects.State[slot] = 0;
        _entities.Mode[entity] = 0xFF;
        _objects.Flags[slot] = CapturedFlags;

        // $10BF. Indexed by the byte that just went to $AA30 - see the note on the table.
        _objects.EnemyType[slot] = s_scores[carried];
    }
}
