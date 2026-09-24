// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $1E9F to $1F2B in player-animation.s: state 9 of the $1E3A table, an enemy that runs four pixels
// a step and throws. The spawn gives class 7 this state.
//
// The reference heads it as the player's bubble shooting. It runs for enemy slots only, through
// $1E3A, and what it puts in ObjectTable is type $32, not a bubble.
//
// It never jumps. It runs the way its heading says until a wall turns it, and falls off the end of
// a platform. As it lands it turns to run at its player.
//
// **A throw holds the thing off throwing again for a hundred frames**, in AttackTimer, and puts the
// same hundred in BubbleTimer. Nothing here counts BubbleTimer down, and a throw needs it negative,
// so what brings it back is not in this routine.
internal sealed class EnemyRunner
{
    // $0193 and the arrays beside it, two along, as for EnemyShot.
    private const int ObjectBase = 2;

    // $1EC0. The sixteen slots, from the top down.
    private const int ThrowSlots = 16;

    // $1EAC. Twelve draws in 256 throw.
    private const byte ThrowChance = 0x0C;

    // $1EBC. Not from this low on the screen.
    private const byte LowestThrow = 0xA8;

    // $1ECC. The thrown thing's type, which also goes in its enemy type.
    private const byte ThrownType = 0x32;

    // $1EDB and $1EE6. Not $E9B8's offsets: the throw starts a little right of and above the thing.
    private const byte ThrowXOffset = 0x0C;
    private const byte ThrowYOffset = 0x05;

    // $1EEE.
    private const byte Reload = 0x64;

    // $1F03. The three cells under the thing.
    private const int FloorLeft = 0x51;
    private const int FloorMiddle = 0x52;
    private const int FloorRight = 0x53;

    // $85E8. Bit 0 runs left and bit 1 runs right.
    private const byte LeftBit = 0x01;
    private const byte RightBit = 0x02;

    // $ED12 and $ED18. Four pixels a step, and the cell beside the thing's middle row.
    private const int LeftBeside = 0x28;
    private const int RightBeside = 0x2B;
    private const byte StepLeft = 0xFC;
    private const byte StepRight = 0x04;

    // $EB97 to $EBAF. The heading a landing sets: towards the player, with bit 2 kept.
    private const byte KeptBits = 0x04;

    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly PlayerDescent _descent;
    private readonly BbRandom _random;

    internal EnemyRunner(EntityTable entities, ObjectTable objects, PlayerDescent descent, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(descent);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _objects = objects;
        _descent = descent;
        _random = random;
    }

    // $1E9F. The cell is the one $1E6C found, and target is the slot $1CF3 put in $4B.
    internal void Step(int slot, int target, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        Throw(slot);

        // $1EF6. Saved before anything below can turn it.
        byte heading = _entities.Heading[slot];

        if ((_entities.GroundState[slot] & 0x80) == 0)
        {
            Descend(slot, target, cell, map);
            return;
        }

        // $1F03 and $1F17. No ground, and a fall starts on the same frame.
        if (!EnemyProbes.Blocked(cell, map, FloorLeft, FloorMiddle, FloorRight))
        {
            _entities.GroundState[slot]++;
            Descend(slot, target, cell, map);
            return;
        }

        // $1F1D. Both bits are read from the copy, so a turn by the first does not stop the second.
        if ((heading & LeftBit) != 0)
        {
            EnemyProbes.Sidestep(_entities, slot, cell, map, LeftBeside, StepLeft);
        }

        if ((heading & RightBit) != 0)
        {
            EnemyProbes.Sidestep(_entities, slot, cell, map, RightBeside, StepRight);
        }

        EntityAnimation.Toggle(_entities, slot);
    }

    // $1E9F to $1EF3. The countdown first, then a draw, then the thing must be standing, not holding a
    // throw already, and high enough up.
    private void Throw(int slot)
    {
        if (_entities.AttackTimer[slot] != 0)
        {
            _entities.AttackTimer[slot]--;

            if (_entities.AttackTimer[slot] != 0)
            {
                return;
            }
        }

        if (_random.Next() >= ThrowChance
            || (_entities.GroundState[slot] & 0x80) == 0
            || (_entities.BubbleTimer[slot] & 0x80) == 0
            || _entities.Y[slot] >= LowestThrow)
        {
            return;
        }

        for (int i = ThrowSlots - 1; i >= 0; i--)
        {
            int thrown = i + ObjectBase;

            if ((_objects.Type[thrown] & 0x80) == 0)
            {
                continue;
            }

            _objects.Type[thrown] = ThrownType;
            _objects.EnemyType[thrown] = ThrownType;
            _objects.X[thrown] = (byte)slot;
            _objects.Column[thrown] = (byte)(unchecked((byte)(_entities.X[slot] - ThrowXOffset)) >> 3);
            _objects.Row[thrown] = (byte)(unchecked((byte)(_entities.Y[slot] - ThrowYOffset)) >> 3);
            _entities.AttackTimer[slot] = Reload;
            _entities.BubbleTimer[slot] = Reload;
            return;
        }
    }

    // $EB34. The descent, with $EB94 turned into a BIT so that a landing runs on into $EB97, and $EE4A
    // as the continuation either way.
    private void Descend(int slot, int target, in PlayerCell cell, SolidMap map)
    {
        byte ground = _entities.GroundState[slot];

        _descent.Step(slot, cell, map);

        // $EB97. Landed: run at the player. The `cmp` is unsigned, so level with them is left.
        if (_entities.GroundState[slot] != ground)
        {
            byte towards = _entities.X[slot] >= _entities.X[target] ? LeftBit : RightBit;
            _entities.Heading[slot] = (byte)(towards | (_entities.Heading[slot] & KeptBits));
        }

        EntityAnimation.Toggle(_entities, slot);
    }
}
