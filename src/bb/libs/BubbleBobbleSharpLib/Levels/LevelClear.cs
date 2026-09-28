// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The level clearing, in the tail of $1578 ($16DA-$1772), for a level whose clock has not run out: the last
// enemy made angry, and the last one gone.
//
// A cleared level has $2D at $FF and nine seconds on the clock; what follows those is step 4d's.
internal sealed class LevelClear
{
    private const byte LastEnemy = 1;

    private const byte ClearedSeconds = 9;

    private const byte FoodFromSeed = 0x03;
    private const byte FoodFromClock = 0x08;

    private const byte AngryFully = 0xFF;

    private const byte PopType = 0x3A;
    private const byte BonusPopType = 0x4C;
    private const byte BubbleType = 0x04;
    private const byte FirstBonusType = 0x38;
    private const byte AfterBonusType = 0x42;
    private const byte KeptType1 = 0x44;
    private const byte KeptType2 = 0x46;
    private const byte KeptType3 = 0x4A;

    private const byte FirstBonusLevel = 0;
    private const byte FirstBonusStep = 3;

    private const byte BonusBase = 9;
    private const byte BonusWrap = 0x2E;

    private readonly LevelTimer _timer;
    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly LevelItems _items;
    private readonly BbRandom _random;

    internal LevelClear(LevelTimer timer, EntityTable entities, ObjectTable objects, LevelItems items, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(random);

        _timer = timer;
        _entities = entities;
        _objects = objects;
        _items = items;
        _random = random;
    }

    // $68: which bonus round this level is followed by, or 0 for none.
    internal byte BonusType { get; set; }

    // $59BF: the level a bonus round is due after, and $59FF: how far to the next one ($092A, $095E).
    internal byte BonusLevel { get; set; } = FirstBonusLevel;

    internal byte BonusStep { get; set; } = FirstBonusStep;

    // $0688-$0690, at each level's start.
    internal void Begin(byte subflg)
    {
        BonusType = 0;

        if (BonusLevel != subflg)
        {
            return;
        }

        // The compare leaves the carry set, so the step is added one over.
        BonusLevel = (byte)(BonusLevel + BonusStep + 1);
        BonusStep++;

        int type = (subflg << 1) + BonusBase;

        while (type >= BonusWrap + 1)
        {
            type -= BonusWrap;

            if (type == 0)
            {
                break;
            }
        }

        BonusType = (byte)type;
    }

    // $16DA-$1750, and $1751-$1772 for a level with nothing left in it.
    internal void Update()
    {
        // $1694-$16B5: the READY!! text has run out and comes off the screen, which is not modelled.
        if (_timer.Ready == 0)
        {
            _timer.Ready = LevelTimer.Stopped;
        }

        if ((sbyte)_timer.Hurry >= 0)
        {
            if (_entities.EnemyCount != 0)
            {
                EnrageTheLast();
                return;
            }

            Clear();
            return;
        }

        EnrageTheLast();
    }

    // $16E0-$16E2: with one enemy left, it is angry.
    private void EnrageTheLast()
    {
        if (_entities.EnemyCount == LastEnemy)
        {
            _entities.SetAnger(AngryFully);
        }
    }

    // $16F7-$1750. $58FF (the boss flag) is cleared here too, and is not modelled; nor is $69 ($1E2E's wave),
    // which only the bonus round sets.
    private void Clear()
    {
        _items.CancelSpecial();

        // The C flag into the add is $F1AC's last compare, which VICE showed clear in every one of sixteen clears.
        _items.FoodBase = (sbyte)_items.FoodBase < 0
            ? (byte)(_random.Next() & FoodFromSeed)
            : (byte)(_timer.Seconds + FoodFromClock);

        PopObjects();

        _timer.Frames = LevelTimer.FramesPerSecond;
        _timer.Hurry = 0xFF;
        _timer.Seconds = ClearedSeconds;

        if (BonusType != 0)
        {
            throw new NotSupportedException("the bonus round's entry at $3517 is not translated");
        }
    }

    // $171B-$1750: every object but the three that stay is kept as it was and made a pop.
    private void PopObjects()
    {
        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            byte type = _objects.Type[slot];

            if ((sbyte)type < 0 || type is KeptType1 or KeptType2 or KeptType3)
            {
                continue;
            }

            _objects.EnemyType[slot] = type;

            bool bonus = type is BubbleType or (>= FirstBonusType and < AfterBonusType);

            if (bonus && BonusType != 0)
            {
                _objects.Behaviour[slot] = LevelItems.FoodColour(BonusType);
                _objects.Type[slot] = BonusPopType;
            }
            else
            {
                _objects.Type[slot] = PopType;
            }
        }
    }
}
