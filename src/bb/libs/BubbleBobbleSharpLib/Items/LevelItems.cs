// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Items;

// The two things a level puts down for a player to take: item 0, a bonus food, and item 1, the
// special item. Each has a type, a place and a timer, in zero page as pairs: $52 and $53, $59 to
// $5C, $5D and $5E.
//
// A type with bit 7 set is not there. $2B31 sets both up hidden as the level starts, with a timer
// of a few seconds. The IRQ takes one off each timer every second, and when one runs out $1578
// flips the item's bit 7: it appears, and the next time the timer runs out it is gone for good.
// $2CB7 is the pickup: a player within sixteen pixels scores it, and the special item's effect runs.
//
// $58 is the food's quality. The level's food type is $58 plus up to three, and $1719 sets $58 as a
// level ends from how long the level took - which is Phase 7's level timer, so it is set from outside.
//
// Each item is drawn as four characters whose art $2B31 copies in from sprites_rom, in a colour
// $5F/$60 holds; $1844 draws them. Art and Colour carry both out to the rendition.
//
// **Not here:** the bonus level, $63, which has handlers of its own; and $2D06's store in $B0.
internal sealed class LevelItems
{
    internal const int Count = 2;

    // The bonus level, $63 in SUBFLG.
    private const byte BonusLevel = 0x63;

    // $2BB7 and $2BBB. The best food there is.
    private const byte BestFood = 0x2E;

    // $2BE7, $2BED, $2BEF, $2BF3 and $2BF8. Every ten levels a player who has not died gets the
    // special item $20; at $3A the milestone jumps to $4E and the item is $21.
    private const byte MilestoneStep = 0x0A;
    private const byte MilestoneItem = 0x20;
    private const byte LastMilestone = 0x3A;
    private const byte FinalMilestone = 0x4E;
    private const byte FinalMilestoneItem = 0x21;

    // $2C1E and $2C20. One level in sixteen has one of these two.
    private const byte RareItem = 0x1E;

    // $2C24.
    private const byte OwnItemFrom = 0x07;

    // $2C8C. The playfield's origins, added to the cell times eight.
    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x2D;

    // $1580. A timer is reloaded with eleven seconds as its item appears.
    private const byte ShowSeconds = 0x0B;

    // $2CFB and $2CEB.
    private const byte Reach = 0x10;

    // $2CC9 to $2CD1. The player states that may pick an item up.
    private const byte CarriedState = 0x18;
    private const byte DyingState = 0x0E;
    private const byte RespawnState = 0x10;

    // $2D18 and $2D25. Items from these types on score a byte higher.
    private const byte FoodHighFrom = 0x0F;
    private const byte SpecialHighFrom = 0x18;

    // $2CB9 and $2CBF. The special item that flashes, and the colour bits it flips.
    private const byte FlashingItem = 0x18;
    private const byte FlashBits = 0x05;

    // $A936 and $A94C after it: the food's score, by type.
    private static readonly byte[] s_foodScores =
    [
        0x01, 0x04, 0x05, 0x05, 0x07, 0x07, 0x20, 0x35, 0x50, 0x50, 0x50, 0x70, 0x70, 0x75, 0x85, 0x01,
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, 0x03,
        0x04, 0x04, 0x05, 0x05, 0x06, 0x06, 0x07, 0x07, 0x08, 0x08, 0x09, 0x09, 0x10, 0x10, 0x12,
    ];

    // $A965 and $A985 after it: the special item's score, by type.
    private static readonly byte[] s_specialScores =
    [
        0x10, 0x50, 0x50, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x20,
        0x20, 0x20, 0x20, 0x20, 0x01, 0x50, 0x01, 0x50, 0x01, 0x01, 0x01, 0x01, 0x03, 0x04, 0x05, 0x08,
        0x02, 0x02, 0x05,
    ];

    // $A892 and $A8C1: the food's and the special item's art, a block of sprites_rom, by type.
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

    // $A8E4 and $A913: the food's and the special item's colour RAM byte, by type.
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

    // $52 and $53. Bit 7 set is hidden, and $FF is gone.
    internal Span<byte> Type => _type;

    // $59 and $5A, $5B and $5C. Pixels, in the players' units.
    internal Span<byte> X => _x;

    internal Span<byte> Y => _y;

    // Which block of sprites_rom $2B31 copied in for each item's characters.
    internal ReadOnlySpan<byte> Art => _art;

    // $4E/$50 and $4F/$51, as the level cell they address: where $1844 draws each item's top-left
    // character. Held apart from X and Y, which $2C8C's add can wrap.
    internal ReadOnlySpan<byte> Column => _column;

    internal ReadOnlySpan<byte> Row => _row;

    // $5F and $60. Each item's colour RAM byte.
    internal ReadOnlySpan<byte> Colour => _colour;

    // $5D and $5E. Seconds until the item appears, or goes.
    internal Span<byte> Timer => _timer;

    // $58. What the food type starts from.
    internal byte FoodBase { get; set; }

    // $5B7F. A bit per player who has not died this game; $0989 sets it for those playing.
    internal byte Undying { get; set; }

    // $5B3F. The level the next milestone item waits for, $12 as a game starts.
    internal byte Milestone { get; set; } = 0x12;

    // $2B31, for the level at SUBFLG `number`, counting from zero.
    internal void Setup(Level level, byte number)
    {
        ArgumentNullException.ThrowIfNull(level);

        if (number == BonusLevel)
        {
            throw new NotSupportedException("the bonus level's items at $2B9E are not translated");
        }

        // $2B41 and $2B78. The cells times eight; $2C8C adds the origins below.
        _x[0] = (byte)(level.FoodDrop.X << 3);
        _y[0] = (byte)(level.FoodDrop.Y << 3);
        _x[1] = (byte)(level.PowerupSpawn.X << 3);
        _y[1] = (byte)(level.PowerupSpawn.Y << 3);
        _column[0] = (byte)level.FoodDrop.X;
        _row[0] = (byte)level.FoodDrop.Y;
        _column[1] = (byte)level.PowerupSpawn.X;
        _row[1] = (byte)level.PowerupSpawn.Y;

        // $2BB0. The `adc` takes the carry $E9EA left, which `and` does not touch.
        int food = (_random.Next() & 0x03) + FoodBase + (_random.Carry ? 1 : 0);
        food = Math.Min(food, BestFood);
        _type[0] = (byte)(food | 0x80);
        _art[0] = s_foodArt[food];
        _colour[0] = s_foodColours[food];
        FoodBase = 0;

        // $2C32.
        int special = Special(number);
        _type[1] = (byte)(special | 0x80);
        _art[1] = s_specialArt[special];
        _colour[1] = s_specialColours[special];

        // $2C5B. Three to ten seconds before the food, and one to sixteen before the special item.
        _timer[0] = (byte)((_random.Next() & 0x07) + 3);
        _timer[1] = (byte)((_random.Next() & 0x0F) + 1);

        // $2C8C. The Y add takes the X add's carry.
        for (int i = Count - 1; i >= 0; i--)
        {
            int x = _x[i] + LeftEdge;
            _x[i] = (byte)x;
            _y[i] = (byte)(_y[i] + TopEdge + (x > 0xFF ? 1 : 0));
        }
    }

    // $06C6, once a second from the IRQ while the level clock runs.
    internal void Tick()
    {
        _timer[0]--;
        _timer[1]--;
    }

    // $1578 to $15C8. An item whose timer has run out appears, or goes. Going ends the whole routine
    // for this pass: $158F's `rts` also skips the other item and the timers after this.
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

    // $2CB7. Player two first, and for each the special item before the food. A taken item stays
    // until $1578 clears it, so both players can take the same one in the same pass.
    internal void Collect(byte number)
    {
        // $2CB7. Only while the item shows: the comparison takes bit 7 with it.
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

    // $2BDB to $2C32. The milestone item, or one seeded by the level, or now and then a rare one.
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

        // $2BFC. The level's own item: the RNG seeded with the level number, and put back after.
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

    // $2CFF.
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
