// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// What the clock running out does ($15E1-$15FA): the hurry-up, the Baron, or, on a level that has been cleared,
// the end of the level.
//
// The first time, $2D is 0: the hurry-up makes every enemy angry and gives the players ten more seconds. The
// second, with $2D at 1, is the Baron: one for each player in the game, in the object slot of the player's number,
// and the clock stopped. On a cleared level, with $2D negative, it is the end of the level.
internal sealed class LevelEnd
{
    private const byte HurryUpSeconds = 0x0A;
    private const byte AngryFully = 0xFF;

    private const byte PopType = 0x3A;
    private const byte FirstMovableType = 0x18;
    private const byte AfterMovableType = 0x24;
    private const byte FirstBaronType = 0x2E;
    private const byte BaronFoodSeed = 0xFF;

    private const int WaitLimit = 0x4000;

    private static readonly byte[] s_spawnColour = [0x05, 0x03];

    private static readonly byte[] s_spawnColumn = [0x02, 0x1B];
    private static readonly byte[] s_spawnRow = [0x05, 0x19];

    private readonly LevelTimer _timer;
    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly BubblePop _pop;
    private readonly LevelItems _items;
    private readonly BbRandom _random;

    internal LevelEnd(
        LevelTimer timer,
        EntityTable entities,
        ObjectTable objects,
        BubblePop pop,
        LevelItems items,
        BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(pop);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(random);

        _timer = timer;
        _entities = entities;
        _objects = objects;
        _pop = pop;
        _items = items;
        _random = random;
    }

    internal static ReadOnlySpan<byte> SpawnColour => s_spawnColour;

    // $21: the level is over, and $0A6A takes the game on to the next.
    internal byte Complete { get; private set; }

    // $A813, $A814: the flash a player had when a bonus round began, and has again after it. The bonus
    // round (step 7) is what sets them.
    internal byte[] SavedFlash { get; } = new byte[PlayerTable.Capacity];

    // $05C5 clears $21 at each level's start.
    internal void Begin() => Complete = 0;

    // $15E4-$15FA.
    internal void Update()
    {
        if (_timer.Hurry == 0)
        {
            HurryUp();
        }
        else if ((sbyte)_timer.Hurry > 0)
        {
            SpawnBarons();
        }
        else
        {
            Finish();
        }
    }

    // $15E8-$15F4, and $3AB8 for the anger. The HURRY UP! scroll and its tune are not modelled.
    private void HurryUp()
    {
        _entities.SetAnger(AngryFully);
        _timer.Hurry++;
        _timer.Seconds = HurryUpSeconds;
    }

    // $1621-$1693. Whatever is in the Barons' two object slots pops first, and the game waits for it to go. Then
    // each slot has a column and a row chosen by chance, and the slot of each player in the game gets a Baron there.
    // The clock stops, with the level's time on it, until the last Baron goes ($1490).
    private void SpawnBarons()
    {
        PopSlots();
        WaitForSlots();

        _items.FoodBase = BaronFoodSeed;

        Span<byte> column = stackalloc byte[PlayerTable.Capacity];
        Span<byte> row = stackalloc byte[PlayerTable.Capacity];

        for (int slot = PlayerTable.Capacity - 1; slot >= 0; slot--)
        {
            _objects.SubY[slot] = 0;
            _objects.SubX[slot] = 0;
            _objects.Variant[slot] = 0;
            _objects.EnemyType[slot] = 0;

            column[slot] = s_spawnColumn[_random.Next() & 1];
            _objects.Column[slot] = column[slot];
            row[slot] = s_spawnRow[_random.Next() & 1];
            _objects.Row[slot] = row[slot];

            if (_entities.State[slot] != 0)
            {
                _objects.Type[slot] = PopType;
            }
        }

        WaitForSlots();

        for (int slot = PlayerTable.Capacity - 1; slot >= 0; slot--)
        {
            if (_entities.State[slot] != 0)
            {
                _objects.Column[slot] = column[slot];
                _objects.Row[slot] = row[slot];
                _objects.Type[slot] = (byte)(FirstBaronType + (slot << 1));
            }
        }

        _timer.Frames = LevelTimer.Stopped;
        _timer.Hurry = 0;
        _timer.Seconds = _timer.Start;
    }

    // $1AE8-$1B01: whatever is in the two slots is kept, unless it is not one of the movable types, and is popped.
    private void PopSlots()
    {
        for (int slot = PlayerTable.Capacity - 1; slot >= 0; slot--)
        {
            byte type = _objects.Type[slot];

            if ((sbyte)type < 0)
            {
                continue;
            }

            _objects.EnemyType[slot] = type is >= FirstMovableType and < AfterMovableType ? type : (byte)0;
            _objects.Type[slot] = PopType;
        }
    }

    // $1B02-$1B18: the objects go on being updated, at least once, until both slots are free.
    private void WaitForSlots()
    {
        int passes = 0;

        do
        {
            _pop.Update();
        }
        while (!SlotsFree() && ++passes < WaitLimit);

        if (!SlotsFree())
        {
            throw new InvalidOperationException("the Barons' object slots never freed at $1B02");
        }
    }

    private bool SlotsFree()
    {
        for (int slot = PlayerTable.Capacity - 1; slot >= 0; slot--)
        {
            if ((sbyte)_objects.Type[slot] >= 0)
            {
                return false;
            }
        }

        return true;
    }

    // $15FA-$1620. Each player in the game, player 2 first, has to be playing or still flashing from a respawn, and
    // one flashing goes on to play. A player in any other state makes the level wait a pass; otherwise it is over.
    private void Finish()
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            byte state = _entities.State[player];

            if (state is 0 or PlayerFrame.PlayingState)
            {
                continue;
            }

            if (state != PlayerRespawn.RespawningState)
            {
                return;
            }

            _entities.State[player] = PlayerFrame.PlayingState;
            _entities.FlashTimer[player] = SavedFlash[player];
            _entities.Colour[player] = s_spawnColour[player];
        }

        Complete++;
    }
}
