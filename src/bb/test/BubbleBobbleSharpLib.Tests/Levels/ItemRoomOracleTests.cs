// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The bonus room of the special items 16, 17 and 18 (`$2F8D`), against the real game run on SharpKind.Emulation.
//
// **An item's effect is entered directly** at the start of a pass of each level, as `$2D51`'s `jsr` would (an intervention:
// the stack gets a return to the pass, and the program counter the effect's address), and the game runs on through the
// routine's frames. When the room has begun to wait for the players (`$3193`'s loop), the items it placed are read from
// `$4600` (X) and `$4700` (Y), `$597F` says how many, and the port's placement on the level's map is compared.
//
// Level 43 is left out, where the row count comes to nought and the game looks for items in whatever follows the map in memory.
[Trait("Level", "Integration")]
public sealed class ItemRoomOracleTests
{
    private const int Count = 0x597F;
    private const int Xs = 0x4600;
    private const int Ys = 0x4700;
    private const int Loop = 0x31A2;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Asset("zones.json"));

    private static readonly HashSet<int> s_left = [43];

    [Fact]
    public void PlacesTheItemsOfEveryLevelWhereTheGameDoes()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();

        List<string> wrong = [];
        byte stack = machine.Cpu.S;

        for (int level = 1; level < LevelStore.Count; level++)
        {
            if (level > 1)
            {
                // The room leaves the stack as its loop had it, and the level's end runs from a pass's start.
                machine.Cpu.S = stack;
                machine.Poke(0x37, 0);
                machine.EnterLevel(level);
            }

            if (s_left.Contains(level))
            {
                continue;
            }

            EnterRoom(machine, 0x2F7D);

            List<(int, int)> expected =
                [.. Enumerable.Range(0, machine.Peek(Count)).Select(i => (machine.Peek(Xs + i) + 0, machine.Peek(Ys + i) + 0))];
            SolidMap map = SolidMap.Build(s_levels.Level(level), s_zones.Zones(level));
            List<(int, int)> placed = [.. ItemRoom.Place(map, (byte)(level - 1)).Select(item => (item.X + 0, item.Y + 0))];

            if (expected.Count == 0 || !expected.SequenceEqual(placed))
            {
                wrong.Add($"level {level}: the game placed {expected.Count}, the port {placed.Count}");
            }
        }

        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong));
    }

    // What the players take. Each turn of the collection (`$31B4`-`$3229`) is bracketed with player 1 put near an item (an
    // intervention: at the item, then a little way from it, then out of reach); the port takes the same turn from the bytes
    // before, and the items still there, how many each player has, the score and the count are compared.
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(12)]
    public void TakesTheItemsAsTheGameDoes(int level)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();

        if (level != 1)
        {
            machine.EnterLevel(level);
        }

        EnterRoom(machine, 0x2F7D);

        SolidMap map = SolidMap.Build(s_levels.Level(level), s_zones.Zones(level));
        int taken = 0;

        (int X, int Y)[] offsets = [(0, 0), (9, 0), (0, -9), (15, 15), (16, 0), (0, 16), (-15, 12), (40, 40)];

        for (int turn = 0; turn < 60 && machine.Peek(Count) != 0; turn++)
        {
            (int dx, int dy) = offsets[turn % offsets.Length];
            int index = Enumerable.Range(0, 0x60).First(i => machine.Peek(Xs + i) != 0);

            Capture capture = machine.Bracket(
                0x31B4,
                0x3229,
                300_000,
                m =>
                {
                    m.Poke(0xBA, (byte)(m.Peek(Xs + index) + dx));
                    m.Poke(0xC2, (byte)(m.Peek(Ys + index) + dy));
                });

            (ItemRoom room, Scores scores) = Room(capture.Before, map, level);

            Assert.Equal(capture.After[Count], (byte)room.Remaining);
            Assert.Equal(capture.After[0x31], room.Counts[0]);
            Assert.Equal(capture.After[0x32], room.Counts[1]);

            // The bonus for the last item is paid after `$3229`, where the game leaves the loop for the results.
            for (int i = 0; i < 6 && room.Remaining != 0; i++)
            {
                Assert.Equal(capture.After[0x0400 + i], scores.Bytes[i]);
            }

            for (int i = 0; i < room.Items.Count; i++)
            {
                Assert.Equal(capture.After[Xs + i], room.Items[i].X);
                Assert.Equal(capture.After[Ys + i], room.Items[i].Y);
            }

            taken += capture.Before[Count] - capture.After[Count];
        }

        Assert.True(taken > 3, $"only {taken} items were taken");
    }

    // The results. Player 1 is put on each item in turn until none is left, and the game runs to where it starts its wait
    // (`$3107`); the port takes the same frames, and the score and the text on the screen are compared. With no item taken
    // the clock runs out instead.
    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(1, false)]
    public void ShowsTheResultsAsTheGameDoes(int level, bool takeAll)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();

        if (level != 1)
        {
            machine.EnterLevel(level);
        }

        EnterRoom(machine, 0x2F7D);

        SolidMap map = SolidMap.Build(s_levels.Level(level), s_zones.Zones(level));
        byte[] before = [.. machine.Ram];

        EntityTable entities = new();
        RamImage.Load(before, entities);
        Scores scores = new();

        for (int i = 0; i < 6; i++)
        {
            scores.Add(i, before[0x0400 + i]);
        }

        LevelTimer timer = new() { Seconds = ItemRoom.Seconds, Frames = LevelTimer.Stopped };
        LevelItems items = new(entities, scores, new ItemEffects(new PlayerTable(), entities), new(new ScriptedRandomSource([])));
        ItemRoom room = new(entities, scores, timer, items, () => { });
        room.Begin(ItemRoom.FirstEffect, (byte)(level - 1), map);
        room.Restore(before.AsSpan(Xs, 0x60), before.AsSpan(Ys, 0x60), before[Count], 0, 0);

        for (int frame = 0; frame < 4000 && machine.Peek(Count) != 0; frame++)
        {
            if (!takeAll)
            {
                machine.RunFrames(1400);
                break;
            }

            int index = Enumerable.Range(0, 0x60).First(i => machine.Peek(Xs + i) != 0);
            machine.Poke(0xBA, machine.Peek(Xs + index));
            machine.Poke(0xC2, machine.Peek(Ys + index));
            entities.Slot(0).X = machine.Peek(Xs + index);
            entities.Slot(0).Y = machine.Peek(Ys + index);
            machine.RunFrames(1);
        }

        machine.RunTo(0x3107, 20_000_000);

        Assert.Equal(0x3107, machine.Cpu.PC);

        // The port takes as many frames as the game did items' worth: one for each.
        while (room.Phase == ItemRoom.Stage.Playing)
        {
            if (takeAll)
            {
                RoomItem next = room.Items.First(item => item.X != 0 || item.Y != 0);
                entities.Slot(0).X = next.X;
                entities.Slot(0).Y = next.Y;
            }
            else
            {
                timer.Seconds = 0;
            }

            room.Frame();
        }

        Assert.Equal(ItemRoom.Stage.Results, room.Phase);

        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(machine.Peek(0x0400 + i), scores.Bytes[i]);
        }

        Assert.Equal(machine.Peek(0x31), room.Counts[0]);
        Assert.Equal(machine.Peek(0x32), room.Counts[1]);

        foreach (RoomText line in room.Lines)
        {
            Assert.Contains(line.Text, Screen(machine, line.Row, line.Column, line.Text.Length));
        }
    }

    // The characters of a stretch of the screen, as text: the game's screen code is the ASCII less $20, the digits being
    // the codes 0 to 9 and a blank $20.
    private static IEnumerable<string> Screen(Rebb64Machine machine, int row, int column, int length)
    {
        foreach (int page in new[] { 0x5000, 0x5400 })
        {
            yield return string.Concat(Enumerable.Range(0, length).Select(i =>
            {
                int code = machine.Peek(page + (row * 40) + column + i);

                return code < 10 ? (char)('0' + code) : (char)(code + 0x20);
            }));
        }
    }

    // The port's room as the game's stood before a turn of the collection: the items placed and the players where they were.
    private static (ItemRoom Room, Scores Scores) Room(byte[] before, SolidMap map, int level)
    {
        EntityTable entities = new();
        RamImage.Load(before, entities);
        Scores scores = new();

        for (int i = 0; i < 6; i++)
        {
            scores.Add(i, before[0x0400 + i]);
        }

        LevelTimer timer = new() { Seconds = before[0x2A], Frames = before[0x2B] };
        LevelItems items = new(entities, scores, new ItemEffects(new PlayerTable(), entities), new(new ScriptedRandomSource([])));
        ItemRoom room = new(entities, scores, timer, items, () => { });

        room.Begin(ItemRoom.FirstEffect, (byte)(level - 1), map);
        room.Restore(before.AsSpan(Xs, 0x60), before.AsSpan(Ys, 0x60), before[Count], before[0x31], before[0x32]);
        room.Frame();

        return (room, scores);
    }

    // Runs the effect from the start of a pass to the point where the room waits for the players.
    private static void EnterRoom(Rebb64Machine machine, int effect)
    {
        machine.RunFrames(20);
        machine.RunTo(Rebb64Machine.Pass, 5_000_000);

        int stack = machine.Cpu.S;
        machine.Poke(0x0100 + stack, (Rebb64Machine.Pass - 1) >> 8);
        machine.Poke(0x0100 + stack - 1, (Rebb64Machine.Pass - 1) & 0xFF);
        machine.Cpu.S = (byte)(stack - 2);
        machine.Cpu.X = 0;
        machine.Cpu.PC = effect;

        machine.RunTo(Loop, 5_000_000);
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
