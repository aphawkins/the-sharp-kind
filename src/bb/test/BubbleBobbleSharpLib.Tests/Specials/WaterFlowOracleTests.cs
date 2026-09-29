// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;
using BubbleBobbleSharpLib.Tests.Enemies;
using BubbleBobbleSharpLib.Tests.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using BubbleBobbleSharpLib.Tests.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Specials;

// The water bubble's flow (`$1B40`) and state `$0D`, against the real game run on SharpKind.Emulation.
//
// **An intervention starts the water.** At the first pass of a level, `$4D` is written `$FF` and the eleven cells of
// `$A74B` and `$A756` are set to the place a bubble popped at, as `$3E0A`-`$3E27` would: nothing on the level's own
// account makes water without a player popping a special bubble. From there the game runs by itself.
//
// `$1B40` is bracketed from its entry to `$06F0`, where the IRQ's `jsr` returns, with interrupts off. Every call's RAM
// before goes into the port and the port's `WaterFlow.Update` runs; what it leaves is compared with the game's: `$4D`, the
// twenty-two cells, every entity byte and the enemy count, and `$26`/`$27` (the walk's direction is a roll).
//
// Twice in each run a second intervention puts a thing in the water's way (`Catch`): an enemy, a player and a
// released enemy (state `$0A`) are each moved to the cell before the head, so that `$1BD2`'s take is exercised.
//
// State `$0D`'s handler is bracketed as one slot's turn in `$1CBD`, `$1CDB` to `$1D24`, and run through
// `WaterFlow.Follow` for an enemy and `PlayerFrame.Slot` for a player.
[Trait("Level", "Integration")]
public sealed class WaterFlowOracleTests
{
    private const int Mask = 0x4D;
    private const int Columns = 0xA74B;
    private const int Rows = 0xA756;
    private const int Cells = 11;
    private const int Random = 0x26;

    private const int StreamStart = 0x1B40;
    private const int StreamReturn = 0x06F0;
    private const int SlotStart = 0x1CDB;
    private const int SlotEnd = 0x1D24;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Asset("zones.json"));

    public static TheoryData<int, byte, byte> Streams() => new()
    {
        { 1, 16, 8 },
        { 1, 5, 6 },
        { 1, 24, 12 },
        { 2, 16, 6 },
        { 5, 10, 8 },
    };

    [Theory]
    [MemberData(nameof(Streams))]
    public void FlowsExactlyAsTheGameDoes(int number, byte column, byte row)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        SolidMap map = SolidMap.Build(s_levels.Level(number), s_zones.Zones(number));
        Rebb64Machine machine = Start(number);

        machine.Poke(Mask, 0xFF);
        machine.Poke(Columns, [.. Enumerable.Repeat((byte)(column + 1), Cells)]);
        machine.Poke(Rows, [.. Enumerable.Repeat((byte)(row + 1), Cells)]);

        int carried = 0;

        for (int call = 0; call < 400 && machine.Peek(Mask) != 0; call++)
        {
            int catchAt = call;
            Capture capture = machine.Bracket(StreamStart, StreamReturn, 300_000, m => Catch(m, catchAt));

            EntityTable entities = new();
            RamImage.Load(capture.Before, entities);

            BbRandom random = new(new ScriptedRandomSource(capture.Timer.Select(b => (int)b)))
            {
                Low = capture.Before[Random],
                High = capture.Before[Random + 1],
            };

            WaterFlow water = new(entities, random) { Mask = capture.Before[Mask] };
            capture.Before.AsSpan(Columns, Cells).CopyTo(water.Columns);
            capture.Before.AsSpan(Rows, Cells).CopyTo(water.Rows);

            water.Update(map);

            Assert.Equal(capture.After[Mask], water.Mask);
            Assert.Equal(Convert.ToHexString(capture.After, Columns, Cells), Convert.ToHexString(water.Columns.ToArray()));
            Assert.Equal(Convert.ToHexString(capture.After, Rows, Cells), Convert.ToHexString(water.Rows.ToArray()));

            string expected = RamImage.Entities(capture.After);
            string actual = RamImage.Entities(entities);

            Assert.True(expected == actual, $"call {call}: {RamImage.EntityDifference(expected, actual)}");
            Assert.Equal(capture.After[Random], random.Low);
            Assert.Equal(capture.After[Random + 1], random.High);

            carried += Enumerable.Range(0, EntityTable.Capacity).Count(s => capture.After[0xB2 + s] == WaterFlow.CarriedState);
        }

        Assert.True(carried > 0, "nothing was carried, so the take was not compared");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    public void CarriesAThingExactlyAsTheGameDoes(int slot)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        SolidMap map = SolidMap.Build(s_levels.Level(1), s_zones.Zones(1));
        Rebb64Machine machine = Start(1);

        machine.Poke(Mask, 0xFF);
        machine.Poke(Columns, [.. Enumerable.Repeat((byte)17, Cells)]);
        machine.Poke(Rows, [.. Enumerable.Repeat((byte)9, Cells)]);

        int compared = 0;

        for (int call = 0; call < 120 && machine.Peek(Mask) != 0; call++)
        {
            // A stream call, to move the water on; the slot's turn follows in the same frames.
            machine.Bracket(StreamStart, StreamReturn, 300_000, m => Catch(m, call == 3 ? 3 : -1));

            if (machine.Peek(0xB2 + slot) != WaterFlow.CarriedState)
            {
                continue;
            }

            Capture turn = machine.Bracket(SlotStart, SlotEnd, 300_000, when: m => m.Cpu.X == slot);

            EntityTable entities = new();
            RamImage.Load(turn.Before, entities);

            BbRandom random = new(new ScriptedRandomSource([]));
            WaterFlow water = new(entities, random);
            turn.Before.AsSpan(Columns, Cells).CopyTo(water.Columns);
            turn.Before.AsSpan(Rows, Cells).CopyTo(water.Rows);

            if (slot >= PlayerTable.Capacity)
            {
                TestEnemies.Frame(entities, new ObjectTable(), random, new Scores(), new(new()), water)
                    .Step(slot, turn.Before[0x08], map);
            }
            else
            {
                Frame(entities, water).Slot(slot, turn.Before[0x85E8 + slot], turn.Before[0x08], map);
            }

            string expected = RamImage.Entities(turn.After);
            string actual = RamImage.Entities(entities);

            Assert.True(expected == actual, $"call {call}: {RamImage.EntityDifference(expected, actual)}");
            compared++;
        }

        Assert.True(compared > 0, "nothing was carried, so the slot was not compared");
    }

    // Puts an enemy, a player and a released enemy at the cell before the head, on the call given: an intervention.
    private static void Catch(Rebb64Machine machine, int call)
    {
        if (call != 3)
        {
            return;
        }

        byte pixelX = (byte)((machine.Peek(Columns + 9) << 3) + 0x14);
        byte pixelY = (byte)((machine.Peek(Rows + 9) << 3) + 0x05);

        foreach (int slot in new[] { 2, 0 })
        {
            machine.Poke(0xBA + slot, (byte)(pixelX + 2));
            machine.Poke(0xC2 + slot, (byte)(pixelY + 3));
        }

        machine.Poke(0xB2 + 2, 2);
        machine.Poke(0xB2 + 0, 1);
        machine.Poke(0x87A0, 0xFF);
    }

    private static PlayerFrame Frame(EntityTable entities, WaterFlow water)
    {
        BubbleBlow blow = TestBlow.Of(entities);
        PlayerSteer steer = new(entities, blow);
        PlayerDescent descent = new(entities);

        return new(
            entities,
            new PlayerMovement(entities, blow, TestBlow.NoRings()),
            new PlayerJump(entities, new PlayerDrift(entities, steer, TestBlow.NoRings()), new PlayerLanding(entities, descent)),
            new PlayerFall(entities, steer),
            blow,
            water);
    }

    private static Rebb64Machine Start(int number)
    {
        Rebb64Machine machine = new();

        machine.StartOnePlayer();

        if (number != 1)
        {
            machine.EnterLevel(number);
        }

        return machine;
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
