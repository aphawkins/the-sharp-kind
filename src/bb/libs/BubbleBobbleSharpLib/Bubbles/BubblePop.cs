// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

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

    private const byte ItemType = 0x06;
    private const byte PlatformType = 0x0A;
    private const byte VanishType = 0x08;

    private static readonly byte[] s_baronSide = [0x02, 0x00];

    private static readonly byte[] s_letterBits = [0x01, 0x02, 0x04, 0x08, 0x10, 0x20];

    private static readonly byte[] s_chainScores = [0x05, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80];

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;
    private readonly PlayerTable _players;
    private readonly FoodDrop _food;
    private readonly Scores _scores;

    private readonly byte[] _chain = new byte[PlayerTable.Capacity];
    private readonly byte[] _chainBefore = new byte[PlayerTable.Capacity];

    private readonly byte[] _drawn = new byte[ObjectTable.Capacity];

    private readonly byte[] _drawnSubY = new byte[ObjectTable.Capacity];

    internal BubblePop(ObjectTable objects, EntityTable entities, PlayerTable players, FoodDrop food, Scores scores)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(food);
        ArgumentNullException.ThrowIfNull(scores);

        _objects = objects;
        _entities = entities;
        _players = players;
        _food = food;
        _scores = scores;
    }

    internal Span<byte> Chain => _chain;

    internal Span<byte> Drawn => _drawn;

    internal Span<byte> DrawnSubY => _drawnSubY;

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
        byte type = _objects.Type[slot];

        _drawnSubY[slot] = _objects.SubY[slot];
        _drawn[slot] = type switch
        {
            <= LastFloatingType => Entry(type, slot),
            TouchedType or ExpiredType or PopFrom => PopFrame(0, slot),
            PopMiddle or PopLate => PopFrame(4, slot),
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
                _objects.Type[slot] = FloatingType;
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
                _objects.Type[slot] = PopMiddle;
                break;
            case PopMiddle:
                _objects.Type[slot] = PopLate;
                break;
            case PopLate:
                _objects.Type[slot] = PopLast;
                break;
            case PopLast:
                _objects.Type[slot] = GoneType;
                break;
            case GoneType:
                _objects.Type[slot] = ObjectTable.FreeType;
                _objects.Column[slot] = 0;
                _objects.Row[slot] = 0;
                break;
        }
    }

    private byte Entry(byte type, int slot) => (byte)((type * 2) + _objects.SubX[slot]);

    private byte PopFrame(int frameBase, int slot) => (byte)(PopFrameBase + frameBase + _objects.SubX[slot]);

    private int? Shot(byte type, int slot)
    {
        int turn = _objects.SubX[slot];

        return type switch
        {
            ShotType => ShotCell + Turn(turn, ShotTurns, type),
            ClassSixType => ClassSixCell + Turn(turn, ClassSixTurns, type),
            ClassFiveType => ClassFiveCell + Turn(turn, ShotTurns, type),
            ThrownType => ThrownCell,
            _ when Baron.Is(type) => BaronCell + s_baronSide[slot] + ((Counter >> 1) & 0x01),
            _ => null,
        };
    }

    private void Carry(int slot)
    {
        int entity = ((_objects.Type[slot] - CapturedFrom) >> 1) + 2;

        _entities.Frame[entity] = _objects.EnemyType[slot];
        _entities.FlashTimer[entity] = 0;
        _entities.X[entity] = _objects.X[slot];
        _entities.Y[entity] = _objects.Y[slot];
    }

    private void Spread(int slot)
    {
        if (_objects.State[slot] != 0)
        {
            return;
        }

        for (int other = ObjectTable.Capacity - 1; other >= 0; other--)
        {
            if (other == slot
                || _objects.Type[other] >= ChainBelow
                || Distance.Absolute(_objects.X[slot], _objects.X[other]) >= ChainRange
                || Distance.Absolute(_objects.Y[slot], _objects.Y[other]) >= ChainRange)
            {
                continue;
            }

            _objects.EnemyType[other] = _objects.Type[other];
            _objects.Type[other] = TouchedType;
            _objects.Direction[other] = _objects.Direction[slot];
        }
    }

    private void Release(int slot)
    {
        byte held = _objects.EnemyType[slot];

        if (held is < CapturedFrom or >= CapturedBelow)
        {
            Burst(slot, held);
            return;
        }

        int enemy = (held - CapturedFrom) >> 1;
        int player = _objects.Direction[slot];

        _entities.State[enemy + 2] = _objects.Variant[slot];
        _food.Kill(enemy);

        _entities.SpriteEnable |= (byte)(1 << (enemy + 2));

        _chain[player]++;
        _entities.AttackTimer[enemy + 2] = _chain[player];

        _objects.Type[slot] = GoneType;
    }

    private void Burst(int slot, byte held)
    {
        if (_objects.Type[slot] == TouchedType)
        {
            int player = _objects.Direction[slot];
            byte score = held is >= SpecialFrom and < SpecialBelow ? SpecialScore : BubbleScore;

            _scores.Add(Scores.Last(player & 0x01), score);

            if (held is >= LetterFrom and < LetterBelow)
            {
                Letter(player, held);
            }
        }

        if (held is ItemType or PlatformType or VanishType)
        {
            throw new NotSupportedException($"special bubble ${held:X2}'s pop at $3E02 is not translated");
        }

        _objects.Type[slot] = PopFrom;
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
