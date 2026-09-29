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
    private const byte FirstBaronType = 0x2E;
    private const byte BaronFoodSeed = 0xFF;

    private static readonly byte[] s_spawnColour = [0x05, 0x03];

    private static readonly byte[] s_spawnColumn = [0x02, 0x1B];
    private static readonly byte[] s_spawnRow = [0x05, 0x19];

    private readonly LevelTimer _timer;
    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly SlotPop _slots;
    private readonly LevelItems _items;
    private readonly BbRandom _random;
    private readonly RoundState _round;

    internal LevelEnd(
        LevelTimer timer,
        EntityTable entities,
        ObjectTable objects,
        BubblePop pop,
        LevelItems items,
        BbRandom random,
        RoundState? round = null)
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
        _slots = new(objects, pop);
        _items = items;
        _random = random;
        _round = round ?? new();
    }

    internal static ReadOnlySpan<byte> SpawnColour => s_spawnColour;

    // $21: the level is over, and $0A6A takes the game on to the next.
    internal byte Complete { get; private set; }

    // $A813, $A814: the flash a player had when a bonus round began, and has again after it. The bonus
    // round (step 7) is what sets them.
    internal byte[] SavedFlash { get; } = new byte[PlayerTable.Capacity];

    // `$1621`: the Barons' call puts the giant bubble's sweeps (`$58FF`) to nothing.
    internal GiantBubble? Giant { get; set; }

    // How many times the Barons have been called, for the song ($1621's `ldy #$43`, the round start's).
    internal int BaronCalls { get; private set; }

    // $05C5 clears $21 at each level's start.
    internal void Begin() => Complete = 0;

    // $32E9's `dec $21` (`ExtendBonus`): the level is over, whatever the enemies say.
    internal void Skip() => Complete--;

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
        BaronCalls++;
        _slots.Pop();
        _slots.Wait();
        Giant?.Cancel();

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

        _slots.Wait();

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

            if (state != _round.Respawn)
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
