// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;

namespace BubbleBobbleSharpLib.Bubbles;

internal sealed class BubblePop
{
    internal const byte NotDrawn = 0xFF;

    private const byte BlownType = 0x16;
    private const byte FloatingType = 0x00;
    private const byte TouchedType = 0x34;
    private const byte ExpiredType = 0x3A;
    private const byte PopFrom = 0x3C;
    private const byte PopMiddle = 0x3E;
    private const byte PopLate = 0x40;
    private const byte PopLast = 0x38;
    private const byte GoneType = 0x36;
    private const byte Hidden = 0x80;
    private const byte LastFloatingType = 0x14;

    private const int PopFrameBase = 44;

    private const byte ShotType = 0x28;
    private const byte ClassSixType = 0x2A;
    private const byte ClassFiveType = 0x2C;
    private const byte ThrownType = 0x32;

    private const int BaronCell = 52;
    private const int ClassSixCell = 56;
    private const int ShotCell = 58;
    private const int ClassFiveCell = 62;
    private const int ThrownCell = 66;
    private const int FireCell = 67;
    private const int DropFrames = 4;
    private const int BoltCell = 71;
    private const int FruitCell = 72;
    private const byte FruitType = 0x4C;
    private const byte PlayfieldColourRam = 0x0D;

    private const int ShotTurns = 4;
    private const int ClassSixTurns = 2;

    private const byte ChainBelow = 0x24;
    private const byte ChainRange = 0x18;

    private const byte CapturedFrom = 0x18;
    private const byte CapturedBelow = 0x24;

    private const byte BubbleScore = 0x01;
    private const byte SpecialScore = 0x0A;
    private const byte SpecialFrom = 0x06;
    private const byte SpecialBelow = 0x0C;

    private const byte LetterFrom = 0x0C;
    private const byte LetterBelow = 0x16;

    private const byte WaterType = 0x06;
    private const byte ThunderItem = 0x0A;
    private const byte FireItem = 0x08;

    private const byte FireType = 0x24;
    private const byte ThunderType = 0x26;
    private const byte ThunderShotType = 0x44;

    private const byte LeftThrow = 0x84;
    private const byte RightThrow = 0x04;
    private const int GridOrigin = 0x14;
    private const int GridReturn = 0x13;
    private const byte GridMask = 0xF8;

    private static readonly byte[] s_baronSide = [0x02, 0x00];

    private static readonly byte[] s_letterBits = [0x01, 0x02, 0x04, 0x08, 0x10, 0x20];

    private static readonly byte[] s_chainScores = [0x05, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80];

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;
    private readonly PlayerTable _players;
    private readonly FoodDrop _food;
    private readonly Scores _scores;
    private readonly WaterFlow _water;

    private readonly byte[] _chain = new byte[PlayerTable.Capacity];
    private readonly byte[] _chainBefore = new byte[PlayerTable.Capacity];

    private readonly byte[] _drawn = new byte[ObjectTable.Capacity];

    private readonly byte[] _drawnSubY = new byte[ObjectTable.Capacity];

    private readonly byte[] _drawnColumn = new byte[ObjectTable.Capacity];

    private readonly byte[] _drawnRow = new byte[ObjectTable.Capacity];

    private readonly byte[] _drawnColour = new byte[ObjectTable.Capacity];

    internal BubblePop(ObjectTable objects, EntityTable entities, PlayerTable players, FoodDrop food, Scores scores, WaterFlow water)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(food);
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(water);

        _objects = objects;
        _entities = entities;
        _players = players;
        _food = food;
        _scores = scores;
        _water = water;
    }

    internal Span<byte> Chain => _chain;

    internal Span<byte> Drawn => _drawn;

    internal Span<byte> DrawnSubY => _drawnSubY;

    // The place each slot was drawn at, which the movers change later in the same pass: the picture is the one
    // made here, at the start of the pass, and shows until the next.
    internal Span<byte> DrawnColumn => _drawnColumn;

    internal Span<byte> DrawnRow => _drawnRow;

    // Each slot's colour RAM byte, which is the level's own for everything but the fruit.
    internal Span<byte> DrawnColour => _drawnColour;

    // The block of item art the fruit is drawn from ($4230, loaded by $3517 from the round's food).
    internal byte FruitArt { get; set; }

    internal byte Counter { get; set; }

    internal void Update()
    {
        Counter++;
        _chain.CopyTo(_chainBefore, 0);

        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            Step(slot);
        }

        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            Score(player);
        }
    }

    private static int Turn(int turn, int turns, byte type) => turn < turns
        ? turn
        : throw new NotSupportedException($"a shot of type ${type:X2} with $A9C4 {turn} reads past its table");

    private void Step(int slot)
    {
        byte type = _objects.Slot(slot).Type;

        _drawnSubY[slot] = _objects.Slot(slot).SubY;
        _drawnColumn[slot] = _objects.Slot(slot).Column;
        _drawnRow[slot] = _objects.Slot(slot).Row;
        _drawnColour[slot] = type == FruitType ? _objects.Slot(slot).Behaviour : PlayfieldColourRam;
        _drawn[slot] = type switch
        {
            <= LastFloatingType => Entry(type, slot),
            TouchedType or ExpiredType or PopFrom => PopFrame(0, slot),
            PopMiddle or PopLate => PopFrame(4, slot),
            FireType => (byte)(FireCell + Drop(slot)),
            FruitType => (byte)(FruitCell + FruitArt),
            _ => NotDrawn,
        };

        if (Shot(type, slot) is int shot)
        {
            _drawn[slot] = (byte)shot;
            _drawnSubY[slot] = 0;
        }

        switch (type)
        {
            case >= Hidden:
                break;
            case BlownType:
                _objects.Slot(slot).Type = FloatingType;
                _drawn[slot] = Entry(FloatingType, slot);
                break;
            case >= CapturedFrom and < CapturedBelow:
                Carry(slot);
                break;
            case TouchedType:
                Spread(slot);
                Release(slot);
                break;
            case ExpiredType:
                Release(slot);
                break;
            case PopFrom:
                _objects.Slot(slot).Type = PopMiddle;
                break;
            case PopMiddle:
                _objects.Slot(slot).Type = PopLate;
                break;
            case PopLate:
                _objects.Slot(slot).Type = PopLast;
                break;
            case PopLast:
                _objects.Slot(slot).Type = GoneType;
                break;
            case GoneType:
                _objects.Slot(slot).Type = ObjectTable.FreeType;
                _objects.Slot(slot).Column = 0;
                _objects.Slot(slot).Row = 0;
                break;
        }
    }

    // $E758: the picture is entry $34 or $A9C4 of the tables, one of four.
    private int Drop(int slot)
        => _objects.Slot(slot).SubX < DropFrames
            ? _objects.Slot(slot).SubX
            : throw new NotSupportedException($"a fire drop with $A9C4 {_objects.Slot(slot).SubX} reads past its table");

    private byte Entry(byte type, int slot) => (byte)((type * 2) + _objects.Slot(slot).SubX);

    private byte PopFrame(int frameBase, int slot) => (byte)(PopFrameBase + frameBase + _objects.Slot(slot).SubX);

    private int? Shot(byte type, int slot)
    {
        int turn = _objects.Slot(slot).SubX;

        return type switch
        {
            ShotType => ShotCell + Turn(turn, ShotTurns, type),
            ClassSixType => ClassSixCell + Turn(turn, ClassSixTurns, type),
            ClassFiveType => ClassFiveCell + Turn(turn, ShotTurns, type),
            ThrownType => ThrownCell,
            ThunderType => BoltCell,
            ThunderShotType => ShotCell + Turn(turn, ShotTurns, type),
            _ when Baron.Is(type) => BaronCell + s_baronSide[slot] + ((Counter >> 1) & 0x01),
            _ => null,
        };
    }

    private void Carry(int slot)
    {
        int entity = ((_objects.Slot(slot).Type - CapturedFrom) >> 1) + 2;

        _entities.Slot(entity).Frame = _objects.Slot(slot).EnemyType;
        _entities.Slot(entity).FlashTimer = 0;
        _entities.Slot(entity).X = _objects.Slot(slot).X;
        _entities.Slot(entity).Y = _objects.Slot(slot).Y;
    }

    private void Spread(int slot)
    {
        if (_objects.Slot(slot).State != 0)
        {
            return;
        }

        for (int other = ObjectTable.Capacity - 1; other >= 0; other--)
        {
            if (other == slot
                || _objects.Slot(other).Type >= ChainBelow
                || Distance.Absolute(_objects.Slot(slot).X, _objects.Slot(other).X) >= ChainRange
                || Distance.Absolute(_objects.Slot(slot).Y, _objects.Slot(other).Y) >= ChainRange)
            {
                continue;
            }

            _objects.Slot(other).EnemyType = _objects.Slot(other).Type;
            _objects.Slot(other).Type = TouchedType;
            _objects.Slot(other).Direction = _objects.Slot(slot).Direction;
        }
    }

    private void Release(int slot)
    {
        byte held = _objects.Slot(slot).EnemyType;

        if (held is < CapturedFrom or >= CapturedBelow)
        {
            Burst(slot, held);
            return;
        }

        int enemy = (held - CapturedFrom) >> 1;
        int player = _objects.Slot(slot).Direction;

        _entities.Slot(enemy + 2).State = _objects.Slot(slot).Variant;
        _food.Kill(enemy);

        _entities.SpriteEnable |= (byte)(1 << (enemy + 2));

        _chain[player]++;
        _entities.Slot(enemy + 2).AttackTimer = _chain[player];

        _objects.Slot(slot).Type = GoneType;
    }

    private void Burst(int slot, byte held)
    {
        if (_objects.Slot(slot).Type == TouchedType)
        {
            int player = _objects.Slot(slot).Direction;
            byte score = held is >= SpecialFrom and < SpecialBelow ? SpecialScore : BubbleScore;

            _scores.Add(Scores.Last(player & 0x01), score);

            if (held is >= LetterFrom and < LetterBelow)
            {
                Letter(player, held);
            }
        }

        Arm(slot, held);
    }

    // $3E02-$3E76. `a` is the accumulator the 6502 carries from one test to the next, which is not always the type: a
    // water bubble that cannot start leaves it holding what it last read.
    private void Arm(int slot, byte held)
    {
        int a = held;

        if (a == WaterType)
        {
            a = _water.Mask;

            if (a == 0)
            {
                if (_water.TryStart(_objects.Slot(slot).Column, _objects.Slot(slot).Row))
                {
                    // $3DA9: gone, and nothing drawn.
                    _objects.Slot(slot).Type = GoneType;
                    _drawn[slot] = NotDrawn;
                    return;
                }

                a = (byte)(_objects.Slot(slot).Row + 1);
            }
        }

        switch (a)
        {
            case ThunderItem:
                Throw(slot);
                break;
            case FireItem:
                _objects.Slot(slot).SubY = 0;
                _objects.Slot(slot).Type = FireType;
                _drawn[slot] = (byte)(FireCell + Drop(slot));
                _drawnSubY[slot] = 0;
                break;
            default:
                _objects.Slot(slot).Type = PopFrom;
                break;
        }
    }

    // $3E30-$3E5B: the bubble becomes the bolt, on the grid, thrown the way the player who burst it faces.
    private void Throw(int slot)
    {
        _objects.Slot(slot).Type = ThunderType;

        int difference = _objects.Slot(slot).X - GridOrigin;

        _objects.Slot(slot).X = (byte)(((byte)difference & GridMask) + GridReturn + (difference >= 0 ? 1 : 0));

        int player = _objects.Slot(slot).Direction;

        if (player >= EntityTable.Capacity)
        {
            throw new NotSupportedException(
                $"a bolt from a bubble facing ${player:X2} reads $8520 + ${player:X2}, past the entities, is not translated");
        }

        byte frame = _entities.Slot(player).Frame;

        _objects.Slot(slot).Direction = frame is < 4 or (>= 8 and < 0x0A) ? LeftThrow : RightThrow;
        _drawn[slot] = BoltCell;
        _drawnSubY[slot] = 0;
    }

    private void Letter(int player, byte held)
    {
        int letter = (held - LetterFrom) >> 1;

        if (letter >= 3)
        {
            letter++;
        }

        if (letter == 0 && (_players.ExtendLetters[player] & s_letterBits[0]) != 0)
        {
            letter = 3;
        }

        _players.ExtendLetters[player] |= s_letterBits[letter];
    }

    private void Score(int player)
    {
        byte count = _chain[player];

        if (count == 0 || count != _chainBefore[player])
        {
            return;
        }

        _chain[player] = 0;
        _scores.Add(Scores.Last(player) - 1, s_chainScores[count]);
    }
}
