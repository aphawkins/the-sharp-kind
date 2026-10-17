// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Oracle;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The super bonus room of the special item 32 (`$3621`), against the real game run on SharpKind.Emulation.
//
// **The effect is entered directly** at the start of a pass, as `$2D51`'s `jsr` would (an intervention: the stack gets two
// returns to the pass, one for the effect and one for the routine that called it, which the room's end pops), and the game
// runs on to its loop (`$31A2`). The items are read from `$4648` on (X) and `$4748` (Y), where `$3421` puts them.
[Trait("Level", "Integration")]
public sealed class SuperBonusRoomOracleTests
{
    private const int Count = 0x597F;
    private const int Xs = 0x4648;
    private const int Ys = 0x4748;
    private const int Items = 36;
    private const int Loop = 0x31A2;
    private const int Hook = 0x31B1;
    private const int Turn = 0x31B6;
    private const int TurnEnd = 0x3229;
    private const int SubFlg = 0x10;

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void BuildsTheRoomAsTheGameDoes(int level)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = Enter(level, out int frames);

        Assert.Equal(72, frames);
        Assert.Equal(level, machine.Peek(SubFlg));
        Assert.Equal(0x25, machine.Peek(Count));
        Assert.Equal(PlayerRespawn.SpawnX[0], machine.Peek(0xBA));
        Assert.Equal(0x9D, machine.Peek(0xC2));
        Assert.Equal(SuperBonusRoom.Colours >> 4, machine.Peek(0x1C) & 0x0F);
        Assert.Equal(SuperBonusRoom.Colours & 0x0F, machine.Peek(0x1E) & 0x0F);

        List<RoomItem> placed = SuperBonusRoom.Place();

        for (int i = 0; i < Items; i++)
        {
            Assert.Equal(machine.Peek(Xs + i), placed[i].X);
            Assert.Equal(machine.Peek(Ys + i), placed[i].Y);

            // Each item is `$344A`'s block of the characters $42-$45 at its column and row, in its colour.
            int cell = (placed[i].Row * 40) + placed[i].Column;
            Assert.Equal(0x42, machine.Peek(0x5000 + cell));
            Assert.Equal(SuperBonusRoom.Colour, machine.PeekColour(cell) & 0x0F);
        }

        // The items' characters are a block of the item art (`$9AE0`, 32 bytes a block).
        Assert.True(machine.Ram.Slice(0x4210, 32).SequenceEqual(machine.Ram.Slice(0x9AE0 + (SuperBonusRoom.Art * 32), 32)));

        // The map: the screen without the items ($5400), where the platforms are the characters $10-$1D ($398A).
        SolidMap map = SolidMap.Build(SuperBonusRoom.Map(level), []);

        for (int row = 0; row < SolidMap.Rows; row++)
        {
            for (int column = 0; column < SolidMap.Columns; column++)
            {
                byte character = machine.Peek(0x5400 + (row * 40) + column);
                bool platform = character is >= 0x10 and < 0x1E;

                Assert.True(map[row, column] == platform, $"row {row}, column {column}: ${character:X2}");
            }
        }
    }

    // The room's picture: every screen code of the playfield as the game has it at its loop, without the items ($5400), and the
    // characters $60-$7F ($4300) as super-room-chars.tga holds them, a bit-pair to a pixel.
    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void DrawsThePictureAsTheGameDoes(int level)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = Enter(level, out _);
        PlayfieldModel picture = SuperBonusRoom.Picture();

        Assert.Equal(SuperBonusRoom.Colours, picture.Colours);

        for (int row = 0; row < PlayfieldModel.Rows; row++)
        {
            for (int column = 0; column < PlayfieldModel.Columns; column++)
            {
                Assert.True(
                    machine.Peek(0x5400 + (row * 40) + column) == picture.Character(column, row),
                    $"row {row}, column {column}");
            }
        }

        // An uncompressed colour-mapped TGA of four 32-bit entries, then one index a pixel: 32 cells of 4x8.
        byte[] sheet = File.ReadAllBytes(Path.Combine(
            AppContext.BaseDirectory,
            "Renditions",
            "BubbleBobbleSharp.Renditions.EightBit",
            "Assets",
            "Images",
            "super-room-chars.tga"));
        const int pixels = 18 + (4 * 4);
        const int width = 32 * 4;

        for (int character = 0; character < 32; character++)
        {
            for (int y = 0; y < 8; y++)
            {
                byte bits = machine.Peek(0x4300 + (character * 8) + y);

                for (int x = 0; x < 4; x++)
                {
                    Assert.Equal((bits >> (6 - (x * 2))) & 0x03, sheet[pixels + (y * width) + (character * 4) + x]);
                }
            }
        }

        // The walls, $10-$14, are $60-$64 again.
        Assert.True(machine.Ram.Slice(0x4080, 40).SequenceEqual(machine.Ram.Slice(0x4300, 40)));
    }

    // The giant over the room (`$3700`-`$3711`): its place, frame and colour, and `$5AFF` set, as the port's.
    [Fact]
    public void ShowsTheGiantAsTheGameDoes()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = Enter(1, out _);
        GameLoop loop = new(new(new RandomSource(new Random(1))), 1);
        loop.Boss.SuperRoom();

        Assert.NotEqual(0, machine.Peek(0x5AFF));
        Assert.True(loop.Boss.Active);
        Assert.Equal(machine.Peek(0xBC), loop.Boss.X);
        Assert.Equal(machine.Peek(0xBD), loop.Boss.Y);
        Assert.Equal(machine.Peek(0xBE), loop.Boss.Frame);
        Assert.Equal(machine.Peek(0xC0), loop.Boss.Colour);

        // The room's loop leaves it where it is.
        machine.RunFrames(50);

        Assert.Equal(loop.Boss.X, machine.Peek(0xBC));
        Assert.Equal(loop.Boss.Y, machine.Peek(0xBD));
    }

    // A turn of the loop, with player 1 put on an item (an intervention), against the port's from the bytes before.
    [Fact]
    public void TakesTheItemsAsTheGameDoes()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = Enter(1, out _);

        for (int turn = 0; turn < 6; turn++)
        {
            int index = Enumerable.Range(0, Items).First(i => machine.Peek(Xs + i) != 0);
            int offset = turn * 3;

            Capture capture = machine.Bracket(
                Turn,
                TurnEnd,
                300_000,
                m =>
                {
                    m.Poke(0xBA, (byte)(m.Peek(Xs + index) + offset));
                    m.Poke(0xC2, m.Peek(Ys + index));
                });

            (SuperBonusRoom room, Scores scores, _) = Room(capture.Before, Turn);

            Assert.Equal(capture.After[Count], (byte)room.Remaining);
            Assert.Equal(capture.After[0x31], room.Counts[0]);

            for (int i = 0; i < 6; i++)
            {
                Assert.Equal(capture.After[0x0400 + i], scores.Bytes[i]);
            }

            for (int i = 0; i < Items; i++)
            {
                Assert.Equal(capture.After[Xs + i], room.Items[i].X);
            }
        }
    }

    // The hook (`$3771`), with player 1 at the door on the floor: it goes down only with fewer than 14 on the count.
    [Fact]
    public void LetsAPlayerDownAsTheGameDoes()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = Enter(1, out _);
        List<byte> downs = [];

        foreach (byte remaining in new byte[] { 0x0F, 0x0E, 0x0D, 0x02 })
        {
            Capture capture = machine.Bracket(
                Hook,
                Turn,
                300_000,
                m =>
                {
                    m.Poke(Count, remaining);
                    m.Poke(0xBA, 0x8C);
                    m.Poke(0xC2, 0x9D);
                });

            (_, _, EntityTable entities) = Room(capture.Before, Hook);

            Assert.Equal(capture.After[0xC2], entities.Slot(0).Y);
            downs.Add(capture.After[0xC2]);
        }

        Assert.Equal([0x9D, 0x9D, 0xDD, 0xDD], downs);
    }

    // The end: with one on the count and player 1 down at the door, the colours flash and the level is over, one on.
    [Fact]
    public void EndsTheLevelAsTheGameDoes()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = Enter(1, out _);
        machine.RunTo(Hook, 300_000);
        machine.Poke(Count, 1);
        machine.Poke(0xBA, 0x8C);
        machine.Poke(0xC2, 0xDD);

        (SuperBonusRoom room, _, _) = Room([.. machine.Ram], 0);
        int start = machine.Frame;
        machine.RunTo(0x37B2, 50_000_000);
        int frames = machine.Frame - start;

        int portFrames = 1;

        while (!room.Frame())
        {
            portFrames++;
        }

        Assert.InRange(frames, portFrames, portFrames + 1);
        Assert.Equal(0, machine.Peek(0x37));

        machine.RunTo(Rebb64Machine.Pass, 985_248L * 60);

        Assert.Equal(1, machine.Peek(SubFlg));
        Assert.NotEqual(0, machine.Peek(0x21));
    }

    // The port's room as the game's stood: the items placed and the players where they were. From the hook's turn or the
    // collection's, the port's frame runs that part alone; otherwise none is run.
    private static (SuperBonusRoom Room, Scores Scores, EntityTable Entities) Room(byte[] before, int from)
    {
        EntityTable entities = new();
        RamImage.Load(before, entities);
        Scores scores = new();

        for (int i = 0; i < 6; i++)
        {
            scores.Add(i, before[0x0400 + i]);
        }

        LevelTimer timer = new();
        LevelItems items = new(entities, scores, new ItemEffects(new PlayerTable(), entities), new(new ScriptedRandomSource([])));
        SuperBonusRoom room = new(entities, scores, timer, items, new LevelFlow(timer), () => { });

        byte[] xs = [.. entities.X.ToArray()];
        byte[] ys = [.. entities.Y.ToArray()];
        room.Begin();
        entities.X.Load(xs);
        entities.Y.Load(ys);
        room.Restore(before.AsSpan(Xs, Items), before.AsSpan(Ys, Items), before[Count], before[0x31], before[0x32]);

        if (from == Turn)
        {
            // The hook has had its turn: away from the door, the port's frame is the collection alone.
            room.Frame();
        }
        else if (from == Hook)
        {
            // The collection is bracketed out: with no player in play, the port's frame is the hook alone.
            byte state = entities.Slot(0).State;
            entities.Slot(0).State = 0;
            room.Frame();
            entities.Slot(0).State = state;
        }

        return (room, scores, entities);
    }

    // To the start of a pass of the level, then the effect, run to its loop's first turn.
    private static Rebb64Machine Enter(int level, out int frames)
    {
        Rebb64Machine machine = new();
        machine.StartOnePlayer();

        if (level != 1)
        {
            machine.EnterLevel(level);
        }

        machine.RunFrames(20);
        machine.RunTo(Rebb64Machine.Pass, 5_000_000);

        int stack = machine.Cpu.S;

        for (int i = 0; i < 4; i += 2)
        {
            machine.Poke(0x0100 + stack - i, (Rebb64Machine.Pass - 1) >> 8);
            machine.Poke(0x0100 + stack - i - 1, (Rebb64Machine.Pass - 1) & 0xFF);
        }

        machine.Cpu.S = (byte)(stack - 4);
        machine.Cpu.X = 0;
        machine.Cpu.PC = 0x3621;

        int start = machine.Frame;
        machine.RunTo(Loop, 50_000_000);
        frames = machine.Frame - start;

        return machine;
    }
}
