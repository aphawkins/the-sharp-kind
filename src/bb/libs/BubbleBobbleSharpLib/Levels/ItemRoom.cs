// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The special items of types 16, 17 and 18 (`$2F7D`, `$2F83`, `$2F89`, which go on into `$2F8D`): a bonus room. The level is
// cleared of everything but the players, and every free place on its platforms grows an item, which the players have thirty
// seconds to collect. The game waits in the item's routine for all of it (the IRQ still moves the players), and then
// shows what each player took and pays a bonus if every item was taken.
internal sealed class ItemRoom
{
    internal const int FirstEffect = 16;
    internal const int LastEffect = 18;

    internal const int Seconds = 0x1E;

    private const int FirstRow = 1;
    private const int FirstColumn = 2;
    private const int LastColumn = 28;
    private const int MostItems = 0x55;

    private const int Skip = 0x2A;
    private const int RowsModulo = 0x0A;

    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x35;

    private const byte Reach = 0x10;
    private const byte ItemScore = 0x50;
    private const byte WinnerBonus = 0x01;
    private const byte LoserBonus = 0x50;

    // $A428's frame, and $3169's, which brings the item art in a row a frame from either end.
    private const int SetupFrames = 1;
    private const int ArtFrames = 16;

    // $3074's wipe of the art, and the $C8 frames the results stay up for, and $E49B's last.
    private const int ResultsFrames = 16 + 0xC8 + 1;

    private const byte AfterRoomSeconds = 0x32;

    private const int FirstEnemy = 2;

    private static readonly (byte Art, byte Colour)[] s_kinds = [(0x3F, 0x0D), (0x41, 0x0C), (0x40, 0x0F)];

    private readonly EntityTable _entities;
    private readonly Scores _scores;
    private readonly LevelTimer _timer;
    private readonly LevelItems _items;
    private readonly Action _resetObjects;
    private readonly byte[] _counts = new byte[PlayerTable.Capacity];

    private List<RoomItem> _placed = [];
    private int _frames;

    internal ItemRoom(
        EntityTable entities,
        Scores scores,
        LevelTimer timer,
        LevelItems items,
        Action resetObjects)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(resetObjects);

        _entities = entities;
        _scores = scores;
        _timer = timer;
        _items = items;
        _resetObjects = resetObjects;
    }

    internal enum Stage
    {
        Off = 0,
        Filling = 1,
        Playing = 2,
        Results = 3,
    }

    internal Stage Phase { get; private set; }

    internal bool Active => Phase != Stage.Off;

    // The items still to take, in the order the game placed them, and what became of them.
    internal IReadOnlyList<RoomItem> Items => _placed;

    // `$597F`: how many are left.
    internal int Remaining { get; private set; }

    // `$31`, `$32`: how many each player has taken.
    internal ReadOnlySpan<byte> Counts => _counts;

    // Which block of the item art (item-chars.tga) the items are, and the colour RAM byte they are in.
    internal byte Art { get; private set; }

    internal byte Colour { get; private set; }

    // What is on the screen for the results: text and what a player was paid.
    internal IReadOnlyList<RoomText> Lines { get; private set; } = [];

    // Whether the items are on the screen: from when their art has come in until the results wipe it away.
    internal bool ItemsShown => Phase == Stage.Playing || (Phase == Stage.Results && _frames < ArtFrames);

    // The row pairs of the level the items are looked for in: `$2F98`-`$2FA4`, from the level.
    internal static int RowPairs(byte level)
    {
        int rows = Skip ^ level;

        while (rows >= RowsModulo + 1)
        {
            rows -= RowsModulo;

            if (rows == 0)
            {
                break;
            }
        }

        return rows;
    }

    // `$2FAC`-`$3049`: two rows at a time from the top, and along each from the right, an item wherever the four cells of its
    // block are all free, until there are `$55` or the row pairs are used up (and if that is none, one more).
    internal static List<RoomItem> Place(SolidMap map, byte level)
    {
        ArgumentNullException.ThrowIfNull(map);

        List<RoomItem> items = [];
        int rows = RowPairs(level);
        int row = FirstRow;

        while (row + 1 < SolidMap.Rows)
        {
            for (int column = LastColumn; column >= FirstColumn; column -= 2)
            {
                if (map[row, column] || map[row, column + 1] || map[row + 1, column] || map[row + 1, column + 1])
                {
                    continue;
                }

                items.Add(new(
                    (byte)((column * 8) + LeftEdge),
                    (byte)(((row - FirstRow) * 8) + TopEdge),
                    column,
                    row));
            }

            row += 2;

            if (items.Count >= MostItems)
            {
                break;
            }

            rows = (rows - 1) & 0xFF;

            if (rows == 0)
            {
                if (items.Count != 0)
                {
                    break;
                }

                rows = 1;
            }
        }

        return items;
    }

    // `$A428`: everything but the players goes.
    internal static void ClearLevel(EntityTable entities, LevelItems items, LevelTimer timer, Action resetObjects)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(resetObjects);

        resetObjects();

        for (int slot = FirstEnemy; slot < EntityTable.Capacity; slot++)
        {
            entities.State[slot] = 0;
            entities.X[slot] = 0;
            entities.Y[slot] = 0;
        }

        items.Timer[0] = 0xFF;
        items.Timer[1] = 0xFF;
        items.Type[0] = 0;
        timer.Seconds = 0xFF;
    }

    // `$2F7D`-`$3062`. Everything but the players goes, the items are placed, and the art comes in.
    internal void Begin(int effect, byte level, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        (Art, Colour) = s_kinds[effect - FirstEffect];

        ClearLevel(_entities, _items, _timer, _resetObjects);

        _placed = Place(map, level);
        Remaining = _placed.Count;
        _counts.AsSpan().Clear();
        _entities.EnemyCount = 0;
        Lines = [];

        Phase = Stage.Filling;
        _frames = 0;
    }

    // The room as the game's memory holds it part-way through, which is how a test proves one turn from what came before it: the
    // items' X and Y (`$4600`, `$4700`), how many are left and what each player has.
    internal void Restore(ReadOnlySpan<byte> xs, ReadOnlySpan<byte> ys, int remaining, byte first, byte second)
    {
        for (int i = 0; i < _placed.Count; i++)
        {
            _placed[i] = _placed[i] with { X = xs[i], Y = ys[i] };
        }

        Remaining = remaining;
        _counts[0] = first;
        _counts[1] = second;
        Phase = Stage.Playing;
    }

    // One 50 Hz frame of the room, which is what one turn of `$31A2`'s loop is.
    internal void Frame()
    {
        _frames++;

        switch (Phase)
        {
            case Stage.Filling:
                if (_frames >= SetupFrames + ArtFrames)
                {
                    _timer.Seconds = Seconds;
                    Phase = Stage.Playing;
                }

                break;
            case Stage.Playing:
                Play();
                break;
            case Stage.Results:
                if (_frames >= ResultsFrames)
                {
                    _timer.Seconds = AfterRoomSeconds;
                    _timer.Frames = AfterRoomSeconds;
                    Lines = [];
                    Phase = Stage.Off;
                }

                break;
        }
    }

    // `$3234`: with the clock at zero the room is over, whatever is left. Then `$31B4`-`$3229`: each player in play, player 2
    // first, takes every item within `$10` in both directions of them.
    private void Play()
    {
        if (_timer.Seconds == 0)
        {
            _timer.Frames = LevelTimer.Stopped;
            ShowResults();
            return;
        }

        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_entities.State[player] != PlayerFrame.PlayingState)
            {
                continue;
            }

            for (int index = 0; index < _placed.Count; index++)
            {
                RoomItem item = _placed[index];

                if ((item.X == 0 && item.Y == 0)
                    || Distance.Absolute(_entities.X[player], item.X) >= Reach
                    || Distance.Absolute(_entities.Y[player], item.Y) >= Reach)
                {
                    continue;
                }

                Remaining--;
                _placed[index] = item with { X = 0, Y = 0 };
                _scores.Add(Scores.Last(player), ItemScore);
                _counts[player]++;
            }
        }

        if (Remaining == 0)
        {
            ShowResults();
        }
    }

    // `$3074`-`$3105`.
    private void ShowResults()
    {
        List<RoomText> lines = [new(13, 7, 1, "BONUS")];

        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            int column = player == 0 ? 8 : 17;
            int colour = player == 0 ? 5 : 3;

            lines.Add(new(column, 8, colour, $"500X{_counts[player]:D2}"));

            if (Remaining != 0 || _entities.State[player] == 0)
            {
                continue;
            }

            int other = player ^ 1;
            string bonus;

            if (_counts[player] < _counts[other])
            {
                _scores.Add(Scores.Last(player) - 1, LoserBonus);
                bonus = " 50000";
            }
            else
            {
                _scores.Add(Scores.Last(player) - 2, WinnerBonus);
                bonus = "100000";
            }

            int text = player == 0 ? 5 : 3;

            lines.Add(new(column, 10, text, "PERFECT"));
            lines.Add(new(column, 12, text, bonus));
        }

        Lines = lines;
        Phase = Stage.Results;
        _frames = 0;
    }
}
