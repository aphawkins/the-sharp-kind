// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $0CF2 in enemy-ai.s, `enemy_ai_update`: the walk over all eighteen slots, once a frame.
//
// This is the thing that drives everything in ObjectTable. A bubble is not moved by any bubble
// routine - it is moved by this loop, the same loop every enemy runs. Phase 5 found that out the
// hard way, and the bubble's travel moved to Phase 6 because of it.
//
// The loop itself is small. What it reaches is not, so this class is the walk and its two decisions
// only: which slots are special, and whether a slot has caught a player. The random AI at $0D86 and
// the movement dispatcher at $0F48 are the next items, and both are marked below where the loop
// falls into them.
internal sealed class EnemyAiLoop
{
    // $0CF4's `cmp #$24`. At or above this, the slot is not an ordinary entity and $0D4E routes it
    // by type instead of running any AI on it.
    private const byte SpecialType = 0x24;

    // $0D3F. What a slot becomes the moment it catches a player.
    private const byte CaughtType = 0x34;

    // $0D0A and $0D0E. A player in one of these two states is not there to be caught.
    private const byte PlayerDying = 0x0E;
    private const byte PlayerDead = 0x0F;

    // $0D21 and $0D32. Sixteen pixels in each axis, tested as one unsigned byte after an absolute
    // difference, so the box is square and is not centred on either thing's middle.
    private const byte CatchRange = 0x10;

    // $0D14. The player's X is nudged two pixels right before the subtraction. The reference does
    // this in one axis and not the other, so the catch box sits two pixels left of where a reading
    // of the Y arm alone would put it.
    private const byte PlayerXNudge = 0x02;

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;
    private readonly EnemyDispatcher _dispatcher;

    internal EnemyAiLoop(ObjectTable objects, EntityTable entities, EnemyDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _objects = objects;
        _entities = entities;
        _dispatcher = dispatcher;
    }

    // $0CF2. `ldx #$11`, then down to zero. Seventeen first, which is the same direction the spawn
    // search at $2321 counts in, so the slot a bubble is put in is the slot this reaches first.
    internal void Update(SolidMap map)
    {
        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            Step(slot, map);
        }
    }

    // One turn of the loop body at $0CF4.
    private void Step(int slot, SolidMap map)
    {
        byte type = _objects.Type[slot];

        if (type >= SpecialType)
        {
            // $0D4E. Types $24 upwards get no AI at all; they are routed by value, and none of the
            // three arms is translated.
            //
            // Below $34, the type indexes a jump table of sixteen spawn handlers - and $0D52's
            // `sbc #$23` runs with the carry *clear*, because $0D50's `bcs` fell through. So it
            // takes twenty-four off, not twenty-three, and types $24 to $33 index that table as
            // zero to fifteen. Reading the operand alone puts every one of those handlers out by
            // one. $0D56 also gates the whole table on $67, the game state byte, which nothing
            // here writes yet.
            //
            // At or above $34, only two values mean anything: $44 is Baron Von Blubba, and $0D7F's
            // arm is $4C, which reaches $0EC5. Everything else, a free slot's $FF included, simply
            // moves on to the next slot.
            return;
        }

        if (_objects.State[slot] != 0)
        {
            // $0CFF's `jmp L0F48`, the movement dispatcher.
            _dispatcher.Step(slot, map);
            return;
        }

        // $0D44 and $0D48. A catch moves the loop straight to the next slot; without one it falls
        // into the random AI at $0D86, which needs the RNG at LE9EA and is its own item. The result
        // is discarded here because both ends of it are the next slot.
        _ = Caught(slot);
    }

    // $0D02. Player two first, then player one, and the first of the two within range wins.
    // Returns true where the 6502 takes $0D4A and leaves the slot caught.
    private bool Caught(int slot)
    {
        for (int player = 1; player >= 0; player--)
        {
            if (!InRange(slot, player))
            {
                continue;
            }

            // $0D33. Three stores: which player, what the slot used to be, and that it is caught.
            //
            // The first of them goes to $0193, and that byte already has a meaning here. The blow
            // at $2352 writes the blower's facing into it - $00 or $80 - and $0F98 reads the same
            // byte back and tests its top bit to pick a direction. A player index of 0 or 1 is
            // positive either way, so a caught slot reads as facing right. The overload is the
            // reference's, not a slip here.
            _objects.Direction[slot] = (byte)player;
            _objects.EnemyType[slot] = _objects.Type[slot];
            _objects.Type[slot] = CaughtType;
            return true;
        }

        return false;
    }

    // $0D04 to $0D34. The player must be there to be caught, and then both axes, each as an
    // absolute difference held in one byte.
    private bool InRange(int slot, int player)
        => _entities.State[player] is not (0 or PlayerDying or PlayerDead)
            && Distance.Absolute((byte)(_entities.X[player] + PlayerXNudge), _objects.X[slot]) < CatchRange
            && Distance.Absolute(_entities.Y[player], _objects.Y[slot]) < CatchRange;
}
