// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $EDCD and $ED3B in entity-system.s: an enemy that stops to shoot at its player.
//
// States 3, 7 and 8 of the $1E3A table go to $E9FD, which is $EDCD and then either the walker at
// $EA08 or $ED3B. So classes 1, 5 and 6 walk exactly as class 0 does, and shoot as well.
//
// A shot takes two steps. $EDCD decides to shoot and books a slot in ObjectTable, and BubbleTimer
// becomes a countdown of seven frames. The enemy does not walk while it counts. Then $ED3B puts the
// shot in the slot beside the enemy, or frees the slot again if a wall is in the way.
//
// **Two arrays hold something else while a shot is booked.** BubbleTimer ($8818) is the countdown,
// and RiseCounter ($87A0) is the booked slot. $ED3B puts RiseCounter back to $FF as it fires. Class 5
// then puts the slot in AttackTimer and $2C in BubbleTimer, so it stands still for another 44 frames.
//
// **The ObjectTable indexes are two along.** The 6502 indexes $CC, $A9FC and the rest with a slot
// from 0 to 15, and those are $CA, $A9FA and the rest two bytes along. So a booked slot of 0 is
// ObjectTable slot 2, and ObjectTable slots 0 and 1 are never used for a shot.
internal sealed class EnemyShot
{
    // $0193 and the arrays beside it, two along.
    private const int ObjectBase = 2;

    // $EE25. The sixteen slots, from the top down.
    private const int ShotSlots = 16;

    // $EE31 and $EE3D. The type a booked slot holds, and the flags byte that goes with it.
    private const byte BookedType = 0x42;
    private const byte BookedFlags = 0x09;

    // $EE39. Seven frames between booking and firing.
    private const byte Countdown = 0x07;

    // $EE41. A hundred and fifty frames before the next shot can be booked.
    private const byte Reload = 0x96;

    // $EE00 and $EE11. Not within these of the playfield's edge.
    private const byte RightLimit = 0xF4;
    private const byte LeftLimit = 0x34;

    // $EDE9 and $EE1F. One chance in four to shoot at all, and one in thirty-two more when the player
    // is not on the enemy's own row.
    private const byte ChanceMask = 0x03;
    private const byte OffRowMask = 0x1F;

    // $ED4F and $ED8C. The playfield's corner, as $E9B8 has it.
    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x15;

    // $ED68 and $ED64. The cell beside the enemy's middle row, and the one past it. A shot to the left
    // starts two columns left and one to the right starts two columns right.
    private const int LeftBeside = 0x27;
    private const int RightBeside = 0x2B;
    private const byte LeftStep = 0xFE;
    private const byte RightStep = 0x02;

    // $ED6A and $ED60. What $0195 holds for each.
    private const byte LeftDirection = 0x80;
    private const byte RightDirection = 0x02;

    // $EDA0 to $EDC1. What each class fires. Class 5 also stands still a while.
    private const byte ClassSixState = 0x08;
    private const byte ClassFiveState = 0x07;
    private const byte ClassSixType = 0x2A;
    private const byte ClassFiveType = 0x2C;
    private const byte ShotType = 0x28;
    private const byte ClassFivePause = 0x2C;

    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly BbRandom _random;

    internal EnemyShot(EntityTable entities, ObjectTable objects, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _objects = objects;
        _random = random;
    }

    // $E9FD's test after $EDCD: not negative means a shot is booked and counting down.
    internal bool Booked(int slot) => (_entities.BubbleTimer[slot] & 0x80) == 0;

    // $EDCD. Whether to book a shot this frame. target is the slot $1CF3 put in $4B.
    internal void Aim(int slot, int target)
    {
        if (Booked(slot))
        {
            return;
        }

        if (_entities.AttackTimer[slot] != 0)
        {
            _entities.AttackTimer[slot]--;

            if (_entities.AttackTimer[slot] != 0)
            {
                return;
            }
        }

        // $EDDC. Not in a jump and not falling.
        if ((_entities.RiseCounter[slot] & 0x80) == 0 || (_entities.GroundState[slot] & 0x80) == 0)
        {
            return;
        }

        if ((_random.Next() & ChanceMask) != 0)
        {
            return;
        }

        if (!Facing(slot, target) || !InReach(slot, target))
        {
            return;
        }

        Book(slot);
    }

    // $ED3B. The countdown, and the shot at the end of it.
    internal void Fire(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        _entities.BubbleTimer[slot]--;

        if (_entities.BubbleTimer[slot] != 0)
        {
            return;
        }

        // $ED41. Nothing booked, and the countdown goes round again from one.
        byte booked = _entities.RiseCounter[slot];

        if ((booked & 0x80) != 0)
        {
            _entities.BubbleTimer[slot]++;
            return;
        }

        _entities.RiseCounter[slot] = 0xFF;

        int shot = booked + ObjectBase;
        bool left = _entities.Frame[slot] >= _entities.FrameCount[slot];
        int beside = left ? LeftBeside : RightBeside;

        // $ED60 and $ED6A. Written whether or not the shot goes.
        _objects.Direction[shot] = left ? LeftDirection : RightDirection;

        // $ED73. A wall in either of the two cells beside the enemy, and the slot is freed.
        if (cell.Solid(map, beside) || cell.Solid(map, beside + 1))
        {
            _objects.Type[shot] = ObjectTable.FreeType;
            return;
        }

        // $ED4C and $ED8A. Both subtractions are bytes, so a thing left of the corner wraps.
        byte column = unchecked((byte)(Cell(_entities.X[slot], LeftEdge) + (left ? LeftStep : RightStep)));
        _objects.Column[shot] = column;
        _objects.X[shot] = column;
        _objects.Row[shot] = Cell(_entities.Y[slot], TopEdge);
        _objects.Y[shot] = (byte)slot;
        _objects.SubX[shot] = 0;
        _objects.SubY[shot] = 0;
        _objects.Type[shot] = Load(slot, shot, booked);
    }

    private static byte Cell(byte position, byte edge) => (byte)(unchecked((byte)(position - edge)) >> 3);

    // $EDF1 to $EE13. The subtraction runs with the carry $E9EA left, so an enemy exactly level with
    // its player in X takes either arm, depending on that draw. The enemy must face the player, and
    // be far enough from the edge it faces.
    private bool Facing(int slot, int target)
    {
        byte self = _entities.X[slot];
        bool left = _entities.Frame[slot] >= _entities.FrameCount[slot];

        bool rightOfPlayer = self >= _entities.X[target] + (_random.Carry ? 0 : 1);

        return rightOfPlayer ? left && self >= LeftLimit : !left && self < RightLimit;
    }

    // $EE15. The enemy on the player's row or below it, and off the row only by a further chance.
    private bool InReach(int slot, int target)
    {
        byte self = _entities.Y[slot];
        byte player = _entities.Y[target];

        return self >= player && (self == player || (_random.Next() & OffRowMask) == 0);
    }

    // $EE25 to $EE46. The first free slot from the top, if there is one.
    private void Book(int slot)
    {
        for (int shot = ShotSlots - 1; shot >= 0; shot--)
        {
            if ((_objects.Type[shot + ObjectBase] & 0x80) == 0)
            {
                continue;
            }

            _objects.Type[shot + ObjectBase] = BookedType;
            _entities.RiseCounter[slot] = (byte)shot;
            _entities.BubbleTimer[slot] = Countdown;
            _entities.AttackTimer[slot] = Reload;
            _objects.Flags[shot + ObjectBase] = BookedFlags;
            return;
        }
    }

    // $EDA0 to $EDC3. The type of the shot, by the class that fired it.
    private byte Load(int slot, int shot, byte booked)
    {
        switch (_entities.State[slot])
        {
            case ClassSixState:
                return ClassSixType;
            case ClassFiveState:
                _entities.AttackTimer[slot] = booked;
                _entities.BubbleTimer[slot] = ClassFivePause;
                return ClassFiveType;
            default:
                _objects.SubX[shot] = (byte)(_objects.Direction[shot] & 0x7F);
                return ShotType;
        }
    }
}
