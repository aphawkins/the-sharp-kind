// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;
using BubbleBobbleSharpLib.Tests.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Specials;

// The objects with their own AI (types `$24` to `$32`, `$0D4E`) and the fire they light (`$1319`), against the real game
// run on SharpKind.Emulation, on level 1.
//
// **Every scene is an intervention.** At the first pass, the eighteen object slots are freed and one is written in as the
// type under test, at a chosen cell, going a chosen way (`$0193`), with the fields the game's own creators would set. For
// the fire drop, once a strip is lit an enemy is stood on it (`Stand`), so that `$1372`'s kill is exercised; for the bolt
// and the shots an enemy or a player is moved in front of it once, for the capture and the kill.
//
// Each pass, `$0CF2` (the walk over the objects) is bracketed from its entry to `$0A51`, and `$1319` (the strips) from its
// entry to `$0A1C`, both with interrupts off. The port runs `EnemyAiLoop.Update` and `FireStrips.Update` on the bytes before,
// and everything either can change is compared: the objects, the entities, the strips and the random state.
[Trait("Level", "Integration")]
public sealed class ObjectAiOracleTests
{
    private const int Slot = 7;
    private const int Random = 0x26;
    private const int Freeze = 0x67;
    private const int Counter = 0x597F;

    private const int Columns = 0xA761;
    private const int Rows = 0xA766;
    private const int Lengths = 0xA76B;
    private const int Timers = 0xA770;

    private const int WalkStart = 0x0CF2;
    private const int WalkReturn = 0x0A51;
    private const int StripStart = 0x1319;
    private const int StripReturn = 0x0A1C;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Asset("zones.json"));

    public static TheoryData<string, byte, byte, byte, byte> Scenes() => new()
    {
        // name, type, direction, column, row
        { "fire drop", 0x24, 0x00, 10, 6 },
        { "fire drop, right", 0x24, 0x00, 20, 5 },
        { "fire drop, far left", 0x24, 0x00, 4, 8 },
        { "bolt right", 0x26, 0x04, 6, 12 },
        { "bolt left", 0x26, 0x84, 24, 12 },
        { "shot right", 0x28, 0x02, 8, 14 },
        { "shot left", 0x28, 0x80, 24, 14 },
        { "slow shot right", 0x2A, 0x02, 8, 14 },
        { "slow shot left", 0x2A, 0x80, 24, 14 },
        { "boomerang right", 0x2C, 0x02, 8, 14 },
        { "boomerang left", 0x2C, 0x80, 24, 14 },
        { "thrown", 0x32, 0x00, 12, 6 },
        { "thunder shot right", 0x44, 0x00, 6, 12 },
        { "thunder shot left", 0x44, 0x80, 24, 12 },
        { "fruit", 0x4C, 0x00, 10, 3 },
        { "fruit above a platform", 0x4C, 0x00, 20, 4 },
    };

    [Theory]
    [MemberData(nameof(Scenes))]
    public void BehavesExactlyAsTheGameDoes(string name, byte type, byte direction, byte column, byte row)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        SolidMap map = SolidMap.Build(s_levels.Level(1), s_zones.Zones(1), new EntityTable());
        Rebb64Machine machine = new();
        machine.StartOnePlayer();

        int lit = 0;
        int moved = 0;

        for (int pass = 0; pass < 60; pass++)
        {
            int thisPass = pass;

            Capture strips = machine.Bracket(
                StripStart,
                StripReturn,
                300_000,
                m => Prepare(m, thisPass, type, direction, column, row));
            CompareStrips(strips, name, pass);

            Capture walk = machine.Bracket(WalkStart, WalkReturn, 300_000);
            CompareWalk(walk, map, name, pass);
            moved += RamImage.Objects(walk.Before) == RamImage.Objects(walk.After) ? 0 : 1;

            lit += Enumerable.Range(0, FireStrips.Capacity).Any(s => machine.Peek(Timers + s) < 0x80) ? 1 : 0;
        }

        Assert.True(moved > 0, $"{name}: the object never moved, so nothing was compared");
        Assert.True(type != 0x24 || lit > 0, $"{name}: no strip was lit, so the fire was not compared");
    }

    private static void Prepare(Rebb64Machine machine, int pass, byte type, byte direction, byte column, byte row)
    {
        if (pass == 0)
        {
            for (int slot = 0; slot < ObjectTable.Capacity; slot++)
            {
                machine.Poke(0xCA + slot, ObjectTable.FreeType);
            }

            machine.Poke(0xCA + Slot, type);
            machine.Poke(0xAA42 + Slot, type);
            machine.Poke(0x0193 + Slot, direction);
            machine.Poke(0xA9B2 + Slot, 0);
            machine.Poke(0xA9C4 + Slot, 0);
            machine.Poke(0xA9D6 + Slot, 0);
            machine.Poke(0xDC + Slot, column);
            machine.Poke(0xEE + Slot, row);
            machine.Poke(0xAA0C + Slot, (byte)((column << 3) + 0x14));
            machine.Poke(0xAA1E + Slot, (byte)((row << 3) + 0x15));

            // What `$28`-`$32`'s creators leave: the launch column in X and the owner in Y for a boomerang, the owner in X
            // for a thrown thing.
            if (type == 0x2C)
            {
                machine.Poke(0xAA0C + Slot, column);
                machine.Poke(0xAA1E + Slot, 2);
            }

            if (type == 0x32)
            {
                machine.Poke(0xAA0C + Slot, 2);
            }
        }

        if (pass == 3)
        {
            Meet(machine, type);
        }

        Stand(machine);
    }

    // On the fourth pass something is put where the object is: an enemy for a bolt to knock out, a player for a shot to kill.
    private static void Meet(Rebb64Machine machine, byte type)
    {
        int entity = type is 0x26 or 0x44 ? 2 : 0;
        byte x = machine.Peek(0xAA0C + Slot);
        byte y = machine.Peek(0xAA1E + Slot);

        if (type is 0x24 or 0x2C or 0x32 or 0x4C)
        {
            x = (byte)((machine.Peek(0xDC + Slot) << 3) + 0x18);
            y = (byte)((machine.Peek(0xEE + Slot) << 3) + 0x15);
        }

        machine.Poke(0xB2 + entity, (byte)(entity == 0 ? 1 : 2));
        machine.Poke(0xBA + entity, x);
        machine.Poke(0xC2 + entity, y);
    }

    // While a strip burns, an enemy is put on it: its X within the strip and its Y $10 below the strip's row.
    private static void Stand(Rebb64Machine machine)
    {
        for (int strip = 0; strip < FireStrips.Capacity; strip++)
        {
            if (machine.Peek(Timers + strip) is < 3 or >= 0x80)
            {
                continue;
            }

            machine.Poke(0xB2 + 2, 2);
            machine.Poke(0xBA + 2, (byte)((machine.Peek(Columns + strip) << 3) + 0x14 + 4));
            machine.Poke(0xC2 + 2, (byte)((machine.Peek(Rows + strip) << 3) + 0x15 - 0x10));
        }
    }

    private static void CompareStrips(Capture capture, string name, int pass)
    {
        EntityTable entities = new();
        RamImage.Load(capture.Before, entities);
        BbRandom random = new(new ScriptedRandomSource(capture.Timer.Select(b => (int)b)))
        {
            Low = capture.Before[Random],
            High = capture.Before[Random + 1],
        };

        FoodDrop food = new(entities, new Scores(), random);
        FireStrips fire = Load(capture.Before, entities, food);

        fire.Update(capture.Before[Counter]);

        AssertStrips(capture, fire, name, pass);

        string expected = RamImage.Entities(capture.After);
        string actual = RamImage.Entities(entities);

        Assert.True(expected == actual, $"{name}, pass {pass}: {RamImage.EntityDifference(expected, actual)}");
        Assert.Equal(capture.After[Random], random.Low);
    }

    private static void CompareWalk(Capture capture, SolidMap map, string name, int pass)
    {
        ObjectTable objects = new();
        EntityTable entities = new();
        RamImage.Load(capture.Before, objects);
        RamImage.Load(capture.Before, entities);
        BbRandom random = new(new ScriptedRandomSource(capture.Timer.Select(b => (int)b)))
        {
            Low = capture.Before[Random],
            High = capture.Before[Random + 1],
        };

        Scores scores = new();
        FoodDrop food = new(entities, scores, random);
        FireStrips fire = Load(capture.Before, entities, food);
        BubbleCollision collision = new(objects, entities);
        LevelFlow flow = new(new LevelTimer()) { Freeze = capture.Before[Freeze] };

        for (int i = 0; i < 6; i++)
        {
            scores.Add(i, capture.Before[0x0400 + i]);
        }

        EnemyAiLoop loop = new(
            objects,
            entities,
            new EnemyDispatcher(objects, entities, collision),
            new EntityMover(objects),
            random,
            new Baron(objects, entities, new LevelTimer()),
            new ObjectAi(objects, entities, random, fire, collision, food, scores),
            flow);

        loop.Update(map);

        string expected = RamImage.Objects(capture.After);
        string actual = RamImage.Objects(objects);

        Assert.True(expected == actual, $"{name}, pass {pass}: {RamImage.Difference(expected, actual)}");

        expected = RamImage.Entities(capture.After);
        actual = RamImage.Entities(entities);

        Assert.True(expected == actual, $"{name}, pass {pass}: {RamImage.EntityDifference(expected, actual)}");
        AssertStrips(capture, fire, name, pass);
        Assert.Equal(capture.After[Random], random.Low);
        Assert.Equal(capture.After[Random + 1], random.High);
        Assert.Equal(Convert.ToHexString(capture.After, 0x0400, 6), Convert.ToHexString(scores.Bytes.ToArray()));
    }

    private static FireStrips Load(byte[] ram, EntityTable entities, FoodDrop food)
    {
        FireStrips fire = new(entities, food);

        ram.AsSpan(Columns, FireStrips.Capacity).CopyTo(fire.Columns);
        ram.AsSpan(Rows, FireStrips.Capacity).CopyTo(fire.Rows);
        ram.AsSpan(Lengths, FireStrips.Capacity).CopyTo(fire.Lengths);
        ram.AsSpan(Timers, FireStrips.Capacity).CopyTo(fire.Timers);

        return fire;
    }

    private static void AssertStrips(Capture capture, FireStrips fire, string name, int pass)
    {
        string game = Convert.ToHexString(capture.After, Columns, 4 * FireStrips.Capacity);
        string port = Convert.ToHexString([.. fire.Columns, .. fire.Rows, .. fire.Lengths, .. fire.Timers]);

        Assert.True(game == port, $"{name}, pass {pass}: strips game {game}, port {port}");
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
