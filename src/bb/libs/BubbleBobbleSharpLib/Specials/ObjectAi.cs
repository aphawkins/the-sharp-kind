// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Specials;

// The objects that have their own AI, the types `$24` to `$32` the jump table at `$0D68` reaches (`$0D4E`; the Baron, `$2E`
// and `$30`, is `Baron`). The reference's names for them are wrong; from what they do:
//
//   * `$24`, `$11B7`: a fire drop, from a popped fire bubble. It falls a row every second pass. When it stands on a
//     platform it becomes `$38` and lights a `FireStrips` strip along the platform, as far left and right as two rolls
//     allow (up to 15 cells each way, to where the platform ends), if a strip is free.
//   * `$26`, `$10D6` (the Baron's entry that skips its toggle): a bolt, from a popped thunder bubble. It goes eight pixels a
//     pass the way `$0193` says, for thirty cells, and every enemy it touches is knocked out (`$105B`, then `$1A6F`).
//   * `$28`, `$7C14`: an enemy's shot. It moves a cell a pass along the level, and goes (type `$38`) at a wall.
//   * `$2A`, `$1274`: the same, a cell every second pass (class six's).
//   * `$2C`, `$12B4`: a boomerang (class five's): out along the level, and back when it meets a wall, until it is home.
//   * `$32`, `$12F9`: a thrown thing that falls a row a pass.
//   * All of them but the drop and the bolt kill a player they touch (`$152B`).
internal sealed class ObjectAi
{
    internal const byte FireType = 0x24;
    internal const byte BoltType = 0x26;
    internal const byte ShotType = 0x28;
    internal const byte SlowShotType = 0x2A;
    internal const byte BoomerangType = 0x2C;
    internal const byte ThrownType = 0x32;
    internal const byte FruitType = 0x4C;
    internal const byte ThunderShotType = 0x44;

    private const byte GoneType = 0x38;
    private const byte CollectedType = 0x3A;

    private const int RowBias = 4;
    private const int Stride = 40;
    private const int BelowRow = Stride;

    private const byte LastRow = 0x1D;

    private const byte BoltStep = 0x08;
    private const byte BoltLast = 0x1E;

    private const byte LeftDirection = 0x80;

    private const byte DyingState = 0x0E;
    private const byte CatchStateLimit = 0x0D;
    private const byte CatchStateExact = 0x19;
    private const byte CatchRange = 0x10;
    private const byte CatchColumnOrigin = 0x18;
    private const byte CatchRowOrigin = 0x15;

    private const byte Reload = 0x64;

    private const int RightEdge = 0x1E;

    private const byte KillCall = 0x02;

    private const int FruitBelow = 0x78;
    private const byte FruitScore = 0x70;
    private const byte FruitPause = 0x04;
    private const byte FruitXOrigin = 0x14;
    private const byte FruitYOrigin = 0x15;
    private const byte FruitReach = 0x10;

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;
    private readonly BbRandom _random;
    private readonly FireStrips _fire;
    private readonly BubbleCollision _collision;
    private readonly FoodDrop _food;
    private readonly Scores _scores;

    internal ObjectAi(
        ObjectTable objects,
        EntityTable entities,
        BbRandom random,
        FireStrips fire,
        BubbleCollision collision,
        FoodDrop food,
        Scores scores)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(collision);
        ArgumentNullException.ThrowIfNull(food);
        ArgumentNullException.ThrowIfNull(scores);

        _objects = objects;
        _entities = entities;
        _random = random;
        _fire = fire;
        _collision = collision;
        _food = food;
        _scores = scores;
    }

    // What `$B0` was set to by the last bolt to hit an enemy (`$3E`'s `lda #$02`), which nothing translated reads.
    internal byte LastHit { get; private set; }

    internal static bool Handles(byte type)
        => type is FireType or BoltType or ShotType or SlowShotType or BoomerangType or ThrownType or FruitType or ThunderShotType;

    internal void Step(int slot, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        switch (_objects.Slot(slot).Type)
        {
            case FireType:
                Fall(slot, map);
                break;
            case BoltType:
                Bolt(slot);
                break;
            case ShotType:
                _objects.Slot(slot).SubX ^= 0x01;
                _ = Advance(slot, GoneType, map);
                break;
            case SlowShotType:
                Slow(slot, map);
                break;
            case BoomerangType:
                Boomerang(slot, map);
                break;
            case ThrownType:
                Thrown(slot);
                break;
            case FruitType:
                Fruit(slot, map);
                break;
            case ThunderShotType:
                _objects.Slot(slot).SubX ^= 0x01;
                Bolt(slot);
                break;
        }
    }

    private static byte Corner(byte cell, byte origin) => unchecked((byte)((cell << 3) + origin + ((cell >> 5) & 0x01)));

    // The cell at a linear offset from an object's own, in the map's forty-byte rows.
    private static bool SolidAt(SolidMap map, int origin, int offset)
    {
        int linear = origin + offset;
        int row = linear >= 0 ? linear / Stride : -((-linear + Stride - 1) / Stride);

        return map[row, linear - (row * Stride)];
    }

    // The map cell `rows` below and `columns` beside an object's own, as `$7BFE` and a `(pointer),y` reach it.
    private bool Solid(int slot, SolidMap map, int rows, int columns)
        => map[_objects.Slot(slot).Row - RowBias + rows, _objects.Slot(slot).Column + columns];

    // $11B7-$1271.
    private void Fall(int slot, SolidMap map)
    {
        _objects.Slot(slot).SubY ^= 0x04;

        if (_objects.Slot(slot).SubY != 0)
        {
            _objects.Slot(slot).Row++;

            if (_objects.Slot(slot).Row >= LastRow)
            {
                _objects.Slot(slot).Row--;
                _objects.Slot(slot).Type = GoneType;
            }

            return;
        }

        int origin = ((_objects.Slot(slot).Row - RowBias) * Stride) + _objects.Slot(slot).Column;

        if (SolidAt(map, origin, 0) || !SolidAt(map, origin, BelowRow))
        {
            return;
        }

        _objects.Slot(slot).Type = GoneType;

        int strip = _fire.FindFree();

        if (strip < 0)
        {
            return;
        }

        Spread(slot, strip, map, origin);
    }

    // $11F2-$126C: the platform under a drop, scanned left and then right for as far as a roll lets it go.
    private void Spread(int slot, int strip, SolidMap map, int origin)
    {
        int column = _objects.Slot(slot).Column;
        int start = column;
        int count = 0;

        int left = _random.Next() & 0x0F;
        int right = _random.Next() & 0x0F;

        if (right + column >= RightEdge)
        {
            right = -1;
        }

        int pointer = origin;

        do
        {
            pointer--;
            count++;

            if (SolidAt(map, pointer, 0) || !SolidAt(map, pointer, BelowRow))
            {
                break;
            }

            start--;
            left--;
        }
        while (left >= 0);

        if (right >= 0)
        {
            pointer += count + 1;

            while (!SolidAt(map, pointer, 1) && SolidAt(map, pointer, BelowRow + 1))
            {
                count++;
                right--;

                if (right < 0)
                {
                    break;
                }

                pointer++;
            }
        }

        _fire.Light(strip, (byte)start, _objects.Slot(slot).Row, (byte)count);
    }

    // $10D6-$1471, the bolt.
    private void Bolt(int slot)
    {
        if ((_objects.Slot(slot).Direction & 0x80) != 0)
        {
            _objects.Slot(slot).X -= BoltStep;
            _objects.Slot(slot).Column--;

            if (_objects.Slot(slot).Column == 0)
            {
                _objects.Slot(slot).Type = GoneType;
                return;
            }
        }
        else
        {
            _objects.Slot(slot).X += BoltStep;
            _objects.Slot(slot).Column++;

            if (_objects.Slot(slot).Column == BoltLast)
            {
                _objects.Slot(slot).Type = GoneType;
                return;
            }
        }

        byte type = _objects.Slot(slot).Type;

        if (!_collision.Check(slot))
        {
            return;
        }

        int enemy = (_objects.Slot(slot).Type - 0x18) >> 1;

        _entities.Slot(enemy + 2).State = _objects.Slot(slot).Variant;
        _food.Kill(enemy);
        _objects.Slot(slot).Type = type;
        LastHit = KillCall;
    }

    // $0EC5-$0F45, the fruit a level's end rains. It falls a row every second pass, until a platform is three rows under
    // it or the bottom of the level takes it (type $38). A player within $10 pixels takes it for 70 points, and it is gone.
    private void Fruit(int slot, SolidMap map)
    {
        if (_objects.Slot(slot).SubY == 0)
        {
            int origin = ((_objects.Slot(slot).Row - RowBias) * Stride) + _objects.Slot(slot).Column;

            if (!SolidAt(map, origin, FruitBelow) && !SolidAt(map, origin, FruitBelow + 1))
            {
                _objects.Slot(slot).Row++;

                if (_objects.Slot(slot).Row == LastRow)
                {
                    _objects.Slot(slot).Type = GoneType;
                    return;
                }

                _objects.Slot(slot).SubY = FruitPause;
            }
        }
        else
        {
            _objects.Slot(slot).SubY = 0;
        }

        Take(slot);
    }

    // $0EFD-$0F45.
    private void Take(int slot)
    {
        byte x = (byte)((_objects.Slot(slot).Column << 3) + FruitXOrigin);
        byte y = (byte)((_objects.Slot(slot).Row << 3) + FruitYOrigin);

        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_entities.Slot(player).State == 0
                || Distance.Absolute(_entities.Slot(player).X, x) >= FruitReach
                || Distance.Absolute(_entities.Slot(player).Y, y) >= FruitReach)
            {
                continue;
            }

            _scores.Add(Scores.Last(player), FruitScore);
            _objects.Slot(slot).Type = GoneType;
            return;
        }
    }

    // $1274-$1286: every second pass, by which way it goes.
    private void Slow(int slot, SolidMap map)
    {
        _objects.Slot(slot).SubX ^= 0x01;

        bool left = (_objects.Slot(slot).Direction & 0x80) != 0;

        if (_objects.Slot(slot).SubX != (left ? 1 : 0))
        {
            return;
        }

        _ = Advance(slot, CollectedType, map);
    }

    // $12B4-$12F6.
    private void Boomerang(int slot, SolidMap map)
    {
        _ = Advance(slot, CollectedType, map);

        _objects.Slot(slot).SubX = (byte)((_objects.Slot(slot).SubX + 1) & 0x03);

        if (_objects.Slot(slot).Type == CollectedType)
        {
            _objects.Slot(slot).Type = BoomerangType;
            _objects.Slot(slot).Direction ^= LeftDirection;
        }

        if (_objects.Slot(slot).Column != _objects.Slot(slot).X)
        {
            return;
        }

        int owner = _objects.Slot(slot).Y;

        _entities.Slot(owner).AttackTimer = Reload;
        _entities.Slot(owner).BubbleTimer = 0xFF;
        _objects.Slot(slot).Type = GoneType;
    }

    // $12F9-$1315.
    private void Thrown(int slot)
    {
        _objects.Slot(slot).Row++;

        byte type;

        if (Catch(slot))
        {
            type = CollectedType;
        }
        else
        {
            if (_objects.Slot(slot).Row != LastRow)
            {
                return;
            }

            type = GoneType;
        }

        _objects.Slot(slot).Type = type;
        _entities.Slot(_objects.Slot(slot).X).BubbleTimer = 0xFF;
    }

    // $1288-$12AB, the move a shot makes: one cell along the level. A wall ahead ends it, as `collect`.
    private bool Advance(int slot, byte collect, SolidMap map)
    {
        if ((_objects.Slot(slot).Direction & 0x80) != 0)
        {
            if (Solid(slot, map, 2, -1))
            {
                return Collected(slot, collect);
            }

            _objects.Slot(slot).Column--;

            if ((_objects.Slot(slot).Column & 0x80) == 0)
            {
                _ = Catch(slot);
                return false;
            }
        }

        if (Solid(slot, map, 2, 2))
        {
            return Collected(slot, collect);
        }

        _objects.Slot(slot).Column++;
        _ = Catch(slot);
        return false;
    }

    private bool Collected(int slot, byte type)
    {
        _objects.Slot(slot).Type = type;
        _objects.Slot(slot).EnemyType = type;
        return true;
    }

    // $152B-$1573: the first player, the second first, within $10 pixels of the object's corner and in state 1 to $0C or
    // $19, is killed. Returns whether one was.
    private bool Catch(int slot)
    {
        byte x = Corner(_objects.Slot(slot).Column, CatchColumnOrigin);
        byte y = Corner(_objects.Slot(slot).Row, CatchRowOrigin);

        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            byte state = _entities.Slot(player).State;

            if (state is 0 or (not CatchStateExact and >= CatchStateLimit))
            {
                continue;
            }

            if (Distance.Absolute(_entities.Slot(player).X, x) < CatchRange && Distance.Absolute(_entities.Slot(player).Y, y) < CatchRange)
            {
                _entities.Slot(player).State = DyingState;
                return true;
            }
        }

        return false;
    }
}
