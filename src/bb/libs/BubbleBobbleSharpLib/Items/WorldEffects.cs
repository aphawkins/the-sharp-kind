// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;

namespace BubbleBobbleSharpLib.Items;

// The special items whose effects act on the level rather than on the player who took them: the `$2D65` handlers that
// order the giant bubble (`$A653`), schedule a bonus round (`$348A`-`$350C`), start one (`$34A0`), freeze the level (`$2F68`), skip levels (`$7FA4`, `$7FA7`) and clear it of
// enemies (`$2DD7`, with the wipe; `$2ECC` and `$35B0`, with the sprites' show). Those three wait in themselves while the IRQ
// goes on, and leave the wait in `Hold` for the game to run.
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

    private const byte PlayingState = 0x01;

    private readonly LevelClear _clear;
    private readonly LevelFlow _flow;
    private readonly LevelTimer _timer;
    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly FoodDrop _food;
    private readonly LevelItems _items;
    private readonly FireStrips _fire;
    private readonly WaterFlow _water;
    private readonly SpriteShow _show;
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
        FireStrips fire,
        WaterFlow water,
        SpriteShow show,
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
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(water);
        ArgumentNullException.ThrowIfNull(show);

        _clear = clear;
        _flow = flow;
        _timer = timer;
        _entities = entities;
        _objects = objects;
        _food = food;
        _items = items;
        _fire = fire;
        _water = water;
        _show = show;
        Wipe = new(EndWipe);
        _bonus = bonus;
        _giant = giant;
    }

    // `$10`, SUBFLG: the level the game is on, from 0.
    internal byte Level { get; set; }

    // The bonus room of items 16, 17 and 18, which the game starts.
    internal Action<int>? Room { get; set; }

    // The super bonus room of item 32, which the game starts.
    internal Action? SuperRoom { get; set; }

    // `$587F`: the level index (from 0) a skip goes to, or none.
    internal int? SkipTo { get; set; }

    // `$58BF`, set by `$2DD7`: the entity reset has been done, and `$2E79` does nothing more until the next level.
    internal bool Cleared { get; set; }

    // The special item of type 8's wipe of the level, which a level's start puts away.
    internal ScreenWipe Wipe { get; }

    // The wait the last effect is in, for the game to run before it goes on with the pass, or none.
    internal IGameHold? Hold { get; set; }

    // Whether the item's effect is one of these.
    internal static bool Handles(int type)
        => type is 1 or 2 or 7 or 8 or 11 or 15 or (>= 16 and <= 33);

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
            case SuperBonusRoom.Effect:
                SuperRoom?.Invoke();
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
                StartWipe();
                break;
            case 11:
                ClearEnemies();
                _show.Diamonds();
                Hold = _show.Flash;
                break;
            case 29:
                ClearEnemies();
                _show.Giant();
                Hold = _show.Flash;
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
            byte type = _objects.Slot(slot).Type;

            if (type is >= FirstCaught and < CaughtBelow)
            {
                int enemy = (type - FirstCaught) >> 1;

                _entities.Slot(enemy + FirstEnemy).State = _objects.Slot(slot).Variant;
                _entities.SpriteEnable |= (byte)(1 << (enemy + FirstEnemy));
                _food.Kill(enemy);
            }

            _objects.Slot(slot).Type = KilledType;
            _objects.Slot(slot).Column = 0;
            _objects.Slot(slot).Row = 0;
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

    // `$2DD7`-`$2E15`: the water and the fire stop (`$7BA6`), every object is reset (`$2E79`) and no more until the next level
    // (`$58BF`), and the level's two items are hidden (`$52`, `$46`). Then the wipe.
    private void StartWipe()
    {
        _water.Mask = 0;
        _fire.Timers.Fill(FireStrips.Free);
        ResetObjects();
        Cleared = true;
        _items.Type[FoodSlot] = FoodGone;
        _items.Type[SpecialSlot] = FoodGone;
        Wipe.Begin();
        Hold = Wipe;
    }

    // `$2E52`-`$2E78`, after the wipe: a player the water carries is put on its feet, and every enemy killed.
    private void EndWipe()
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_entities.Slot(player).State == CarriedState)
            {
                _entities.Slot(player).State = PlayingState;
            }
        }

        KillEnemies();
    }

    // `$2E79` then `$2E61`: every object is reset, a caught enemy let out to die, and every enemy that is left killed.
    private void ClearEnemies()
    {
        ResetObjects();
        KillEnemies();
    }

    // `$2E61`.
    private void KillEnemies()
    {
        for (int slot = EntityTable.Capacity - 1; slot >= FirstEnemy; slot--)
        {
            byte state = _entities.Slot(slot).State;

            if (state is not 0 && (state < CaughtState || state is CarriedState))
            {
                _food.Kill(slot - FirstEnemy);
            }
        }

        _entities.EnemyCount = 0;
    }
}
