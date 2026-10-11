// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The special item of type 32 (`$3621`): the super bonus room. The level is cleared, a room of the game's own making is drawn
// (`$A660`'s characters, a map by `$E2C3`), and 36 items are put on it in mirrored pairs (`$3748`). The players stand on its
// floor, and the game waits in `$3193`'s loop for them. Its hook (`$3771`, in place of the clock) lets a player standing at
// X `$8C` down into the room under the floor once no more than 12 items are left, and when the last is taken and a player is
// down there at X `$8C` the screen flashes and the level is over, one level on: the routine has counted SUBFLG up (`$3690`).
internal sealed class SuperBonusRoom
{
    internal const int Effect = 32;

    // `$1D`, `$1F`: the room's colours, which `$392A` puts in `$1C` and `$1E`.
    internal const int Colours = 0x6E;

    // Which block of the item art (item-chars.tga) the items are, `$9BC0`'s, and their colour RAM byte (`$A8E4` + `$23`).
    internal const byte Art = 0x07;
    internal const byte Colour = 0x0B;

    // `$597F` starts one more than there are items, so the loop never ends of itself: the hook ends it at 1.
    private const int StartCount = 0x25;

    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x35;
    private const int MirrorColumn = 0x1E;

    private const byte Reach = 0x10;
    private const byte ItemScore = 0x12;

    private const byte FloorY = 0x9D;
    private const byte DoorX = 0x8C;
    private const byte DownY = 0xDD;
    private const int DownBelow = 0x0E;
    private const int LastCount = 1;

    // From the routine's start to its loop's first turn, counted on the game: the screen built, the 20 frames' wait for the song,
    // and the room scrolled in (`$37C9`).
    private const int SetupFrames = 72;

    // `$379A`: sixteen turns of the colours, each after `$7BC3`'s five frames, then `$7BC6`'s ten on a cleared screen.
    private const int FlashTurns = 16;
    private const int FlashTurnFrames = 5;
    private const int ClearedFrames = 10;
    private const byte FlashBit = 0x04;
    private const byte MulticolourBit = 0x08;

    // `$A710` (columns) and `$A722` (rows).
    private static readonly byte[] s_columns =
        [0x06, 0x08, 0x06, 0x08, 0x02, 0x04, 0x06, 0x08, 0x02, 0x04, 0x06, 0x08, 0x02, 0x04, 0x06, 0x08, 0x0A, 0x0C];

    private static readonly byte[] s_rows =
        [0x02, 0x02, 0x05, 0x05, 0x08, 0x08, 0x08, 0x08, 0x0B, 0x0B, 0x0B, 0x0B, 0x16, 0x16, 0x16, 0x16, 0x16, 0x16];

    // The platforms `$E2C3` draws from `$8C00`, rows 1 to 23, as the game has them on the screen.
    private static readonly string[] s_bitmap =
    [
        "##..#......................#..##",
        "##..#......................#..##",
        "##..#......................#..##",
        "##########............##########",
        "##............................##",
        "##............................##",
        "##...#####............#####...##",
        "##............................##",
        "##............................##",
        "###########..........###########",
        "##............................##",
        "##............................##",
        "###########..........###########",
        "##............................##",
        "##............................##",
        "################################",
        "##............................##",
        "##............................##",
        "##............................##",
        "##............................##",
        "##............................##",
        "##............................##",
        "##............................##",
    ];

    // The room's screen codes as the game has them at `$31A2` (`$5400`, without the items): `$E2C3`'s chambers (`$65`-`$6F`) and
    // door (`$70`-`$75`), `$36A2`'s rows of `$77` and `$78`, `$370C`'s blocks of `$46`-`$49`, and the walls in `$10`-`$14`
    // and their edges, which `$3665` has blanked. `$1A` and `$1B`, top left, are the scroll's (`$3870`).
    private static readonly string[] s_screen =
    [
        "1A1B101010101010101010101010101010101010101010101010101010101112",
        "13140F0D100F0D0D0D0D0D0D0D0D0D0D0D0D0D0D0D0D0D0D0D0D0D100F0D1314",
        "11124648100E2020202020202020202020202020202020202020201046481112",
        "13144749100E2020202020202020202020202020202020202020201047491314",
        "111210101010101010100C202020202020202020202010101010101010101112",
        "13140F0D0D0D0D0D0D0D0B20202020202020202020200A0D0D0D0D0D0D0D1314",
        "11120E2020202020202020202020202020202020202020202020202020201112",
        "13140E202010101010100C202020202020202020202010101010100C20201314",
        "11120E20200A0D0D0D0D0B6A6B7777777777772076200A0D0D0D0D0B20201112",
        "13140E2020202020202020202065666767686920202020202020202020201314",
        "11121010101010101010100C6A6B777777772076201010101010101010101112",
        "13140F0D0D0D0D0D0D0D0D0B6566676767676869200A0D0D0D0D0D0D0D0D1314",
        "11120E20202020202020206A6B77777777777720762020202020202020201112",
        "13141010101010101010106A6B77777777777720761010101010101010101314",
        "11120F0D0D0D0D0D0D0D0D6A6B77776C6E777720760A0D0D0D0D0D0D0D0D1112",
        "13140E2020202020202020656667676D6F676768692020202020202020201314",
        "1112101010101010101010101010101010101010101010101010101010101112",
        "1314787777777777777777777777777777777777777777777777777777771314",
        "1112787777777777777777777777777777777777777777777777777777771112",
        "1314787777777777777777777777777777777777777777777777777777771314",
        "1112787777777777777777777777777777777777777777777777777777771112",
        "1314787777777777777777777777777777777777777777777777777777771314",
        "11120E2020202020202020202020707274702020202020202020202020201112",
        "13140E2020202020202020202020717375712020202020202020202020201314",
        "1112101010101010101010101010101010101010101010101010101010101112",
    ];

    private readonly EntityTable _entities;
    private readonly Scores _scores;
    private readonly LevelTimer _timer;
    private readonly LevelItems _items;
    private readonly LevelFlow _flow;
    private readonly Action _resetObjects;
    private readonly byte[] _counts = new byte[PlayerTable.Capacity];

    private List<RoomItem> _placed = [];
    private int _frames;

    internal SuperBonusRoom(
        EntityTable entities,
        Scores scores,
        LevelTimer timer,
        LevelItems items,
        LevelFlow flow,
        Action resetObjects)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(resetObjects);

        _entities = entities;
        _scores = scores;
        _timer = timer;
        _items = items;
        _flow = flow;
        _resetObjects = resetObjects;
    }

    internal enum Stage
    {
        Off = 0,
        Building = 1,
        Playing = 2,
        Flashing = 3,
    }

    internal Stage Phase { get; private set; }

    internal bool Active => Phase != Stage.Off;

    // The items, in the order the game placed them, a taken one at (0, 0).
    internal IReadOnlyList<RoomItem> Items => _placed;

    // `$597F`.
    internal int Remaining { get; private set; }

    // `$31`, `$32`: how many each player has taken.
    internal ReadOnlySpan<byte> Counts => _counts;

    // The room as a level, for its map and its picture: the level being played lends it its number and so its tile.
    internal static Level Map(int number) => new()
    {
        Number = number,
        Colours = Colours,
        Bitmap = s_bitmap,
    };

    // The room's picture, in its colours.
    internal static PlayfieldModel Picture() => PlayfieldModel.SuperBonusRoom(Colours, Convert.FromHexString(string.Concat(s_screen)));

    // `$3748`-`$3762` and `$3421`: from the tables' last entry to their first, an item at the column and its mirror.
    internal static List<RoomItem> Place()
    {
        List<RoomItem> items = [];

        for (int i = s_columns.Length - 1; i >= 0; i--)
        {
            items.Add(Item(s_columns[i], s_rows[i]));
            items.Add(Item(MirrorColumn - s_columns[i], s_rows[i]));
        }

        return items;
    }

    // `$3621`-`$376E`.
    internal void Begin()
    {
        ItemRoom.ClearLevel(_entities, _items, _timer, _resetObjects);
        _timer.Frames = LevelTimer.Stopped;
        _entities.EnemyCount = 0;
        _flow.Begin(Colours);

        // $36DC-$36F0: a player in the game stands on the floor, the other at (0, 0).
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            bool playing = _entities.State[player] != 0;

            if (playing)
            {
                _entities.State[player] = PlayerFrame.PlayingState;
            }

            _entities.X[player] = playing ? PlayerRespawn.SpawnX[player] : (byte)0;
            _entities.Y[player] = playing ? FloorY : (byte)0;
        }

        _placed = Place();
        Remaining = StartCount;
        _counts.AsSpan().Clear();

        Phase = Stage.Building;
        _frames = 0;
    }

    // The room as the game's memory holds it part-way through: the items' X and Y (`$4600`, `$4700`), the count and what each
    // player has.
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

    // One 50 Hz frame, a turn of `$31A2`'s loop. Returns whether the room is over, and with it the level.
    internal bool Frame()
    {
        _frames++;

        switch (Phase)
        {
            case Stage.Building:
                if (_frames >= SetupFrames)
                {
                    Phase = Stage.Playing;
                }

                break;
            case Stage.Playing:
                if (Hook())
                {
                    Phase = Stage.Flashing;
                    _frames = 0;
                }
                else
                {
                    Collect();
                }

                break;
            case Stage.Flashing:
                if (_frames <= FlashTurns * FlashTurnFrames && _frames % FlashTurnFrames == 0)
                {
                    byte one = (byte)(_flow.BackgroundOne ^ FlashBit);
                    _flow.Begin((one << 4) | one | MulticolourBit);
                }

                if (_frames >= (FlashTurns * FlashTurnFrames) + ClearedFrames)
                {
                    Phase = Stage.Off;
                    return true;
                }

                break;
        }

        return false;
    }

    private static RoomItem Item(int column, int row)
        => new((byte)((column * 8) + LeftEdge), (byte)((row * 8) + TopEdge), column, row);

    // `$3771`-`$3794`, player 2 first. Returns whether the room is over.
    private bool Hook()
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_entities.X[player] != DoorX)
            {
                continue;
            }

            byte y = _entities.Y[player];

            if (Remaining == LastCount)
            {
                if (y == DownY)
                {
                    return true;
                }
            }
            else if (Remaining < DownBelow && y == FloorY)
            {
                _entities.Y[player] = DownY;
            }
        }

        return false;
    }

    // `$31B4`-`$3229`, as for the bonus rooms, but with `$12` to the hundreds for each item (`$3200`).
    private void Collect()
    {
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
                _scores.Add(Scores.Last(player) - 1, ItemScore);
                _counts[player]++;
            }
        }
    }
}
