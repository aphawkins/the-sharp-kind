// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Items;

// The special items whose effects act on the level rather than on the player who took them: the `$2D65` handlers that
// order the giant bubble (`$A653`), schedule a bonus round (`$348A`-`$350C`), start one (`$34A0`), freeze the level (`$2F68`), skip levels (`$7FA4`, `$7FA7`) and clear it of
// enemies (`$2DD7`, `$2ECC`). What they draw (the wipe, the flashes, the scattering sprites) and how long they hold the game
// for (the IRQ runs on through it) is not modelled.
internal sealed class WorldEffects
{
    private const byte FreezePasses = 0x87;

    private const byte SkipThree = 3;
    private const byte SkipSeven = 7;
    private const byte LastLevel = 0x63;
    private const byte FinalMilestoneLevel = 0x45;

    private const byte FirstCaught = 0x18;
    private const byte CaughtBelow = 0x24;
    private const byte KilledType = 0x4A;

    private const byte FoodGone = 0xFF;

    private const int FoodSlot = 0;
    private const int SpecialSlot = 1;

    private const byte CaughtState = 0x0B;
    private const byte CarriedState = 0x0D;

    private const int FirstEnemy = 2;

    private readonly LevelClear _clear;
    private readonly LevelFlow _flow;
    private readonly LevelTimer _timer;
    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly FoodDrop _food;
    private readonly LevelItems _items;
    private readonly BonusRound? _bonus;
    private readonly GiantBubble? _giant;

    internal WorldEffects(
        LevelClear clear,
        LevelFlow flow,
        LevelTimer timer,
        EntityTable entities,
        ObjectTable objects,
        FoodDrop food,
        LevelItems items,
        BonusRound? bonus = null,
        GiantBubble? giant = null)
    {
        ArgumentNullException.ThrowIfNull(clear);
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(food);
        ArgumentNullException.ThrowIfNull(items);

        _clear = clear;
        _flow = flow;
        _timer = timer;
        _entities = entities;
        _objects = objects;
        _food = food;
        _items = items;
        _bonus = bonus;
        _giant = giant;
    }

    // `$10`, SUBFLG: the level the game is on, from 0.
    internal byte Level { get; set; }

    // The bonus room of items 16, 17 and 18, which the game starts.
    internal Action<int>? Room { get; set; }

    // `$587F`: the level index (from 0) a skip goes to, or none.
    internal int? SkipTo { get; set; }

    // `$58BF`, set by `$2DD7`: the entity reset has been done, and `$2E79` does nothing more until the next level.
    internal bool Cleared { get; set; }

    // Whether the item's effect is one of these.
    internal static bool Handles(int type)
        => type is 1 or 2 or 7 or 8 or 11 or 15 or (>= 16 and <= 28) or 30 or 31 or 33;

    // `player` is the one who took the item, which none of these use.
    internal void Apply(int type, int player)
    {
        _ = player;

        switch (type)
        {
            case 1:
                _clear.Schedule(0x1B, 0x10, 0);
                break;
            case 21:
                _clear.Schedule(0x1B, 0x20, 0);
                break;
            case 2:
                _clear.Schedule(0x18, 0x20, 1);
                break;
            case 23:
                _clear.Schedule(0x18, 0x30, 1);
                break;
            case 25:
                _clear.Schedule(0x29, 0x30, 2);
                break;
            case 26:
                _clear.Schedule(0x2B, 0x40, 3);
                break;
            case 27:
                _clear.Schedule(0x2C, 0x50, 4);
                break;
            case 15:
                Freeze();
                break;
            case >= ItemRoom.FirstEffect and <= ItemRoom.LastEffect:
                Room?.Invoke(type);
                break;
            case 24:
                _bonus?.Start();
                break;
            case 20:
                Skip(SkipThree);
                break;
            case 22:
                Skip(SkipSeven);
                break;
            case 33:
                // $7C37: to level $45, whichever way that is.
                SkipTo = FinalMilestoneLevel;
                break;
            case 7:
                _giant?.Order();
                break;
            case 8:
                _items.Type[FoodSlot] = FoodGone;
                _items.Type[SpecialSlot] = FoodGone;
                Cleared = false;
                ClearEnemies();
                Cleared = true;
                break;
            case 11:
                ClearEnemies();
                break;
            default:
                // 19, 28, 30 and 31 are a bare `rts`.
                break;
        }
    }

    // `$2E79`.
    internal void ResetObjects()
    {
        if (Cleared)
        {
            return;
        }

        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            byte type = _objects.Type[slot];

            if (type is >= FirstCaught and < CaughtBelow)
            {
                int enemy = (type - FirstCaught) >> 1;

                _entities.State[enemy + FirstEnemy] = _objects.Variant[slot];
                _entities.SpriteEnable |= (byte)(1 << (enemy + FirstEnemy));
                _food.Kill(enemy);
            }

            _objects.Type[slot] = KilledType;
            _objects.Column[slot] = 0;
            _objects.Row[slot] = 0;
        }
    }

    // `$2F68`-`$2F7C`: the level's colours swap, and the enemies keep still for $87 passes with the clock stopped.
    private void Freeze()
    {
        _flow.SwapColours();
        _flow.Freeze = FreezePasses;
        _timer.Frames = LevelTimer.Stopped;
    }

    // `$7FA4`-`$7FB2`: three or seven levels on, no further than the last.
    private void Skip(byte levels) => SkipTo = Math.Min(Level + levels, LastLevel);

    // `$2E79` then `$2E61`: every object is reset, a caught enemy let out to die, and every enemy that is left killed.
    private void ClearEnemies()
    {
        ResetObjects();

        for (int slot = EntityTable.Capacity - 1; slot >= FirstEnemy; slot--)
        {
            byte state = _entities.State[slot];

            if (state is not 0 && (state < CaughtState || state is CarriedState))
            {
                _food.Kill(slot - FirstEnemy);
            }
        }

        _entities.EnemyCount = 0;
    }
}
