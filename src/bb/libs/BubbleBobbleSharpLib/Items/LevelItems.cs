// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Items;

internal sealed class LevelItems
{
    internal const int Count = 2;

    private const byte BonusLevel = 0x63;

    private const byte BestFood = 0x2E;

    private const byte MilestoneStep = 0x0A;
    private const byte MilestoneItem = 0x20;
    private const byte LastMilestone = 0x3A;
    private const byte FinalMilestone = 0x4E;
    private const byte FinalMilestoneItem = 0x21;

    private const byte RareItem = 0x1E;

    private const byte OwnItemFrom = 0x07;

    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x2D;

    private const byte ShowSeconds = 0x0B;

    private const byte Reach = 0x10;

    private const byte CarriedState = 0x18;
    private const byte DyingState = 0x0E;
    private const byte RespawnState = 0x10;

    private const byte FoodHighFrom = 0x0F;
    private const byte SpecialHighFrom = 0x18;

    private const byte FlashingItem = 0x18;
    private const byte FlashBits = 0x05;

    private static readonly byte[] s_foodScores =
    [
        0x01, 0x04, 0x05, 0x05, 0x07, 0x07, 0x20, 0x35, 0x50, 0x50, 0x50, 0x70, 0x70, 0x75, 0x85, 0x01,
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, 0x03,
        0x04, 0x04, 0x05, 0x05, 0x06, 0x06, 0x07, 0x07, 0x08, 0x08, 0x09, 0x09, 0x10, 0x10, 0x12,
    ];

    private static readonly byte[] s_specialScores =
    [
        0x10, 0x50, 0x50, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x20,
        0x20, 0x20, 0x20, 0x20, 0x01, 0x50, 0x01, 0x50, 0x01, 0x01, 0x01, 0x01, 0x03, 0x04, 0x05, 0x08,
        0x02, 0x02, 0x05,
    ];

    private static readonly byte[] s_foodArt =
    [
        0x2C, 0x34, 0x2D, 0x1E, 0x2E, 0x29, 0x2B, 0x0B, 0x00, 0x1B, 0x31, 0x30, 0x02, 0x1C, 0x0B, 0x0E,
        0x26, 0x01, 0x11, 0x19, 0x20, 0x22, 0x21, 0x05, 0x24, 0x26, 0x0C, 0x08, 0x17, 0x25, 0x26, 0x36,
        0x0F, 0x23, 0x06, 0x15, 0x27, 0x2F, 0x04, 0x28, 0x04, 0x07, 0x04, 0x07, 0x07, 0x38, 0x32,
    ];

    private static readonly byte[] s_specialArt =
    [
        0x09, 0x1D, 0x1D, 0x0D, 0x0D, 0x0D, 0x13, 0x13, 0x13, 0x14, 0x14, 0x14, 0x1F, 0x1F, 0x1F, 0x12,
        0x16, 0x16, 0x16, 0x1A, 0x0A, 0x1D, 0x0A, 0x1D, 0x37, 0x35, 0x35, 0x35, 0x14, 0x18, 0x1A, 0x33,
        0x39, 0x39, 0x2A,
    ];

    private static readonly byte[] s_foodColours =
    [
        0x0C, 0x09, 0x09, 0x09, 0x0F, 0x0A, 0x0A, 0x0E, 0x0F, 0x0F, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0F,
        0x0B, 0x0F, 0x0A, 0x0C, 0x0D, 0x0F, 0x0F, 0x0A, 0x0D, 0x09, 0x0A, 0x0A, 0x0F, 0x0F, 0x0F, 0x09,
        0x0F, 0x0A, 0x0C, 0x0B, 0x0F, 0x0A, 0x0F, 0x0F, 0x0A, 0x0F, 0x0D, 0x0D, 0x0C, 0x0F, 0x0F,
    ];

    private static readonly byte[] s_specialColours =
    [
        0x0A, 0x09, 0x0A, 0x0F, 0x0C, 0x0E, 0x0A, 0x0F, 0x0B, 0x0A, 0x0F, 0x0C, 0x0C, 0x0A, 0x0E, 0x0D,
        0x0D, 0x0C, 0x0F, 0x0D, 0x0E, 0x0F, 0x0C, 0x0B, 0x0A, 0x0F, 0x0E, 0x09, 0x0D, 0x0C, 0x0C, 0x0A,
        0x0A, 0x09, 0x0D,
    ];

    private readonly byte[] _type = [0xFF, 0xFF];
    private readonly byte[] _art = new byte[Count];
    private readonly byte[] _colour = new byte[Count];
    private readonly byte[] _column = new byte[Count];
    private readonly byte[] _row = new byte[Count];
    private readonly byte[] _x = new byte[Count];
    private readonly byte[] _y = new byte[Count];
    private readonly byte[] _timer = new byte[Count];

    private readonly EntityTable _entities;
    private readonly Scores _scores;
    private readonly ItemEffects _effects;
    private readonly BbRandom _random;

    internal LevelItems(EntityTable entities, Scores scores, ItemEffects effects, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _scores = scores;
        _effects = effects;
        _random = random;
    }

    internal Span<byte> Type => _type;

    internal Span<byte> X => _x;

    internal Span<byte> Y => _y;

    internal ReadOnlySpan<byte> Art => _art;

    internal ReadOnlySpan<byte> Column => _column;

    internal ReadOnlySpan<byte> Row => _row;

    internal ReadOnlySpan<byte> Colour => _colour;

    internal Span<byte> Timer => _timer;

    internal byte FoodBase { get; set; }

    internal byte Undying { get; set; }

    internal byte Milestone { get; set; } = 0x12;

    // $A8E4.
    internal static byte FoodColour(int food) => s_foodColours[food];

    // $16FB-$170B: a level cleared calls its special item off. One showing has its timer run out; one not yet
    // shown is never shown.
    internal void CancelSpecial()
    {
        if ((sbyte)_type[1] >= 0)
        {
            _timer[1] = 0xFF;
        }
        else
        {
            _type[1] = 0xFF;
        }
    }

    internal void Setup(Level level, byte number)
    {
        ArgumentNullException.ThrowIfNull(level);

        if (number == BonusLevel)
        {
            throw new NotSupportedException("the bonus level's items at $2B9E are not translated");
        }

        _x[0] = (byte)(level.FoodDrop.X << 3);
        _y[0] = (byte)(level.FoodDrop.Y << 3);
        _x[1] = (byte)(level.PowerupSpawn.X << 3);
        _y[1] = (byte)(level.PowerupSpawn.Y << 3);
        _column[0] = (byte)level.FoodDrop.X;
        _row[0] = (byte)level.FoodDrop.Y;
        _column[1] = (byte)level.PowerupSpawn.X;
        _row[1] = (byte)level.PowerupSpawn.Y;

        int food = (_random.Next() & 0x03) + FoodBase + (_random.Carry ? 1 : 0);
        food = Math.Min(food, BestFood);
        _type[0] = (byte)(food | 0x80);
        _art[0] = s_foodArt[food];
        _colour[0] = s_foodColours[food];
        FoodBase = 0;

        int special = Special(number);
        _type[1] = (byte)(special | 0x80);
        _art[1] = s_specialArt[special];
        _colour[1] = s_specialColours[special];

        _timer[0] = (byte)((_random.Next() & 0x07) + 3);
        _timer[1] = (byte)((_random.Next() & 0x0F) + 1);

        for (int i = Count - 1; i >= 0; i--)
        {
            int x = _x[i] + LeftEdge;
            _x[i] = (byte)x;
            _y[i] = (byte)(_y[i] + TopEdge + (x > 0xFF ? 1 : 0));
        }
    }

    internal void Tick()
    {
        _timer[0]--;
        _timer[1]--;
    }

    internal void Update()
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            if ((_timer[i] & 0x80) == 0)
            {
                continue;
            }

            _timer[i] = ShowSeconds;

            if (_type[i] == 0xFF)
            {
                continue;
            }

            _type[i] ^= 0x80;

            if ((_type[i] & 0x80) != 0)
            {
                _type[i] = 0xFF;
                return;
            }
        }
    }

    internal void Collect(byte number)
    {
        if (_type[1] == FlashingItem)
        {
            _colour[1] ^= FlashBits;
        }

        for (int player = 1; player >= 0; player--)
        {
            byte state = _entities.State[player];

            if (state is 0 or not (CarriedState or RespawnState or < DyingState))
            {
                continue;
            }

            for (int item = Count - 1; item >= 0; item--)
            {
                if ((_type[item] & 0x80) != 0
                    || Distance.Absolute(_entities.X[player], _x[item]) >= Reach
                    || Distance.Absolute(_entities.Y[player], _y[item]) >= Reach)
                {
                    continue;
                }

                Take(player, item, number);
            }
        }
    }

    private int Special(byte number)
    {
        if (Undying != 0 && Milestone < number)
        {
            Milestone += MilestoneStep;

            if (Milestone != LastMilestone)
            {
                return MilestoneItem;
            }

            Milestone = FinalMilestone;
            return FinalMilestoneItem;
        }

        byte low = _random.Low;
        byte high = _random.High;
        _random.Low = number;
        _random.High = number;
        int own = (_random.Next() + 1) & 0x1F;
        _random.Low = low;
        _random.High = high;

        int roll = _random.Next() & 0x0F;

        return roll == 0 ? (_random.Next() & 0x01) | RareItem
            : roll >= OwnItemFrom ? own
            : _random.Next() & 0x1F;
    }

    private void Take(int player, int item, byte number)
    {
        _timer[item] = 0xFF;

        int type = _type[item];
        int index = Scores.Last(player);
        byte score;

        if (item == 0)
        {
            score = s_foodScores[type];

            if (type >= FoodHighFrom)
            {
                index--;
            }
        }
        else
        {
            score = s_specialScores[type];

            if (type >= SpecialHighFrom)
            {
                index--;
            }
        }

        _scores.Add(index, score);

        if (item == 1)
        {
            _effects.Apply(type, player);
        }
        else if (number == BonusLevel)
        {
            throw new NotSupportedException("the bonus level's food at $7C3C is not translated");
        }
    }
}
