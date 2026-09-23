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
// The loop itself is small. What it reaches is not, so the movement dispatcher at $0F48 is a class of
// its own. What stays here is the walk, its two decisions - which slots are special, and whether a
// slot has caught a player - and the random AI at $0D86 with the map read at $0E00 that ends it.
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

    // $0D8B's `cmp #$EA`. A draw at or above this skips the neighbour scan and goes straight to $0E00.
    private const byte MapDraw = 0xEA;

    // $0DAE and $0DBF. The same sixteen pixels as the catch, and here with no nudge in either axis.
    private const byte NeighbourRange = 0x10;

    // $0DCB and $0DD4, then $0DE6 and $0DEE. Within this many cells of an edge, the random arm stops
    // being random and turns the thing back towards the middle.
    private const byte NearEdge = 0x04;
    private const byte FarRow = 0x1C;
    private const byte NearColumn = 0x02;
    private const byte FarColumn = 0x1C;

    // $0E02 and $0E08. $0E00 holds the row it reads from to $04 through $1C.
    private const byte TopRow = 0x04;
    private const byte BottomRow = 0x1C;

    // $0E0E to $0E1F. The row table at $AD1E gives $8500 for row four, which is the base
    // EnemyDispatcher's probes reach through $AC01 too - the same four, for the reason given there.
    private const int RowBias = 4;

    // $0E1F's `ldy #$29`. One row below and one column right of the thing's own cell.
    private const int MapOffset = 0x29;
    private const int Stride = 40;

    // $F219 and $F21D. On one level, the store at $F23C is made after a fill loop has left $02 in A.
    private const int OddLevel = 0x48;
    private const byte OddThreshold = 0x02;

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;
    private readonly EnemyDispatcher _dispatcher;
    private readonly EntityMover _mover;
    private readonly BbRandom _random;

    internal EnemyAiLoop(
        ObjectTable objects,
        EntityTable entities,
        EnemyDispatcher dispatcher,
        EntityMover mover,
        BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(mover);
        ArgumentNullException.ThrowIfNull(random);

        _objects = objects;
        _entities = entities;
        _dispatcher = dispatcher;
        _mover = mover;
        _random = random;
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
        // into the random AI at $0D86.
        if (!Caught(slot))
        {
            Wander(slot, map);
        }
    }

    // $0D86. One draw, and a high one goes straight to the map. Otherwise the thing looks for a
    // neighbour, and only a neighbour makes it move at random - with nobody near it falls out of the
    // scan at $0DFE into $0E00 all the same.
    //
    // So a lone thing is driven by the map whatever the draw was, which is what makes a lone bubble
    // the one case that can be checked against a capture. The draw is still made: the generator's
    // state moves on for every slot that reaches here.
    private void Wander(int slot, SolidMap map)
    {
        if (_random.Next() < MapDraw && Neighboured(slot))
        {
            _mover.Step(slot, RandomDirection(slot, map));
            return;
        }

        _mover.Step(slot, MapDirection(slot, map));
    }

    // $0D8F to $0DFE. Seventeen down to zero, passing over itself, anything with an AI state, and
    // anything special - a free slot's $FF included. The first slot inside the box is enough.
    private bool Neighboured(int slot)
    {
        for (int other = ObjectTable.Capacity - 1; other >= 0; other--)
        {
            if (other == slot || _objects.State[other] != 0 || _objects.Type[other] >= SpecialType)
            {
                continue;
            }

            if (Distance.Absolute(_objects.X[slot], _objects.X[other]) < NeighbourRange
                && Distance.Absolute(_objects.Y[slot], _objects.Y[other]) < NeighbourRange)
            {
                return true;
            }
        }

        return false;
    }

    // $0DC2 to $0DFA. A second draw picks the axis, and a third picks the way along it unless the
    // thing is near an edge, when it is sent back towards the middle instead.
    //
    // **The threshold is the level, not $1E.** `cmp #$1E` at $0DC5 is self-modified: $F23C stores into
    // its operand at every level's setup, and what it stores is A, which holds the level number from
    // $F217's `lda SUBFLG`. The $1E the source shows, and the $1E or $E6 $F230 works out into Y, never
    // reach it. So on the first level every draw is at or above zero and the move is always sideways,
    // and a deeper level is more and more often vertical. On the one level $F219 singles out, a fill
    // loop has left $02 in A and that is what is stored.
    private int RandomDirection(int slot, SolidMap map)
    {
        int level = map.Number - 1;
        byte threshold = level == OddLevel ? OddThreshold : (byte)level;

        return _random.Next() >= threshold ? Sideways(slot) : Vertical(slot);
    }

    // $0DE4. `ora #$01` makes any draw right or left.
    private int Sideways(int slot) => _objects.Column[slot] switch
    {
        < NearColumn => EntityMover.Right,
        >= FarColumn => EntityMover.Left,
        _ => _random.Next() | EntityMover.Right,
    };

    // $0DC9. `and #$02` makes any draw up or down.
    private int Vertical(int slot) => _objects.Row[slot] switch
    {
        < NearEdge => EntityMover.Down,
        >= FarRow => EntityMover.Up,
        _ => _random.Next() & EntityMover.Down,
    };

    // $0E00. The current in the cell below and one across, off the collision map's own byte.
    //
    // Clamped at the top, the read lands on row one of the map. Clamped at the bottom it lands one
    // row past the map's last, which SolidMap answers with direction 0; the reference reads whatever
    // is at $88E8 onwards, and nothing has checked what that is.
    private int MapDirection(int slot, SolidMap map)
    {
        int row = Math.Clamp((int)_objects.Row[slot], TopRow, BottomRow);

        return map.Direction(row - RowBias + (MapOffset / Stride), _objects.Column[slot] + (MapOffset % Stride));
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
