// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The giant bubble of the special item of type 7 (`$A653`), against the real game run on SharpKind.Emulation.
//
// **The routines are called directly** (`Invoke`, an intervention) some frames into level 1: a sweep's start, `$1AB6`, and a pass of
// its render, `$198A`. The objects and enemies are put where the sweep meets them (interventions too), and the port's
// `GiantBubble` runs on the bytes before; the object and entity tables, the sprite enable, the sweeps and the phase are compared
// with what the game left. `$E494`, the wait for a frame, is made an `rts` for the length of the start, since a call with the
// interrupts off never sees the frame come.
[Trait("Level", "Integration")]
public sealed class GiantBubbleOracleTests
{
    private const int Start = 0x1AB6;
    private const int Sweep = 0x198A;
    private const int WaitFrame = 0xE494;
    private const int Random = 0x26;
    private const int Sweeps = 0x58FF;
    private const int Phase = 0x593F;
    private const int SpriteEnable = 0x15;

    private const int Type = 0x00CA;
    private const int Column = 0x00DC;
    private const int Row = 0x00EE;
    private const int ObjectX = 0xAA0C;
    private const int ObjectY = 0xAA1E;
    private const int Variant = 0xAA30;
    private const int EnemyType = 0xAA42;
    private const int Direction = 0x0193;

    [Theory]
    [InlineData(20, 0)]
    [InlineData(150, 0)]
    [InlineData(150, 1)]
    [InlineData(150, 2)]
    [InlineData(400, 3)]
    public void StartsASweepExactlyAsTheGameDoes(int frames, int scenario)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(frames);

        byte wait = machine.Peek(WaitFrame);

        machine.Poke(WaitFrame, 0x60);
        machine.Poke(Sweeps, 3);
        machine.Poke(Phase, 0);
        Prepare(machine, scenario);

        byte enabled = machine.PeekVic(SpriteEnable);
        Capture capture = machine.Invoke(Start, 0, 0, 5_000_000);

        machine.Poke(WaitFrame, wait);

        Rig rig = new(capture, enabled);
        int waited = rig.Giant.Start();

        Assert.True(waited >= 21, $"{waited} frames");
        rig.AssertMatches(capture, machine.PeekVic(SpriteEnable), scenario.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(capture.After[Phase], rig.Giant.Phase);
        Assert.Equal(0x46, capture.After[Type]);
        Assert.Equal(0x46, capture.After[Type + 1]);
    }

    [Theory]
    [InlineData(150)]
    [InlineData(400)]
    public void SweepsExactlyAsTheGameDoes(int frames)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(frames);

        machine.Poke(Sweeps, 2);
        machine.Poke(Phase, 4);
        machine.Poke(Type, 0x46);
        machine.Poke(Type + 1, 0x46);
        machine.Poke(Column, 0x18);
        machine.Poke(Row, 2);

        Tally tally = new();

        for (uint pass = 0; pass < 30 && machine.Peek(Phase) != 0; pass++)
        {
            Meet(machine, pass);

            byte enabled = machine.PeekVic(SpriteEnable);
            byte type = machine.Peek(Type);
            Capture capture = machine.Invoke(Sweep, 0, 0, 1_000_000);

            Rig rig = new(capture, enabled);
            rig.Giant.Sweep();
            rig.AssertMatches(capture, machine.PeekVic(SpriteEnable), $"pass {pass}");
            Assert.Equal(capture.After[Sweeps], rig.Giant.Sweeps);
            Assert.Equal(capture.After[Phase], rig.Giant.Phase);

            tally.Count(type, capture);
        }

        Assert.True(
            tally.Moves > 15 && tally.Caught > 1 && tally.Free > 1 && tally.Exits == 1,
            $"{tally.Moves} moves, {tally.Caught} caught, {tally.Free} free, {tally.Exits} exits");
    }

    // The slots and enemies the sweep's cells will meet, by the pass it is on: an intervention.
    private static void Meet(Rebb64Machine machine, uint pass)
    {
        int column = machine.Peek(Column) - 1;
        int row = machine.Peek(Row) + 1;
        int left = (column * 8) + 0x0C;
        int top = (row * 8) + 0x0D;

        if (pass % 3 == 1)
        {
            int slot = 2 + (int)(pass % 16);
            int enemy = (int)(pass % 6);

            machine.Poke(Type + slot, (byte)(0x18 + (enemy * 2)));
            machine.Poke(ObjectX + slot, (byte)(left + 3 + (pass * 2)));
            machine.Poke(ObjectY + slot, (byte)(top + 40));
            machine.Poke(Variant + slot - 2, (byte)(3 + (pass % 5)));
            machine.Poke(EnemyType + slot, (byte)(0x18 + (enemy * 2)));
        }

        if (pass % 4 == 2)
        {
            int enemy = (int)(pass % 6);

            machine.Poke(0xB2 + 2 + enemy, (byte)(1 + (pass % 3)));
            machine.Poke(0xBA + 2 + enemy, (byte)(left + 20));
            machine.Poke(0xC2 + 2 + enemy, (byte)(top + 2));
        }
    }

    // Some of what the start finds in the two slots. A caught enemy is not put in one: the game's wait for it never ends, as the
    // handler of a popping bubble (`$3D77`) frees the slot `$3E` holds, which only a touched bubble's (`$3D2F`) sets.
    private static void Prepare(Rebb64Machine machine, int scenario)
    {
        switch (scenario)
        {
            case 1:
                machine.Poke(Type, 0x00);
                machine.Poke(Column, 8);
                machine.Poke(Row, 12);
                machine.Poke(ObjectX, 0x50);
                machine.Poke(ObjectY, 0x60);
                break;
            case 2:
                machine.Poke(Type, 0x00);
                machine.Poke(Type + 1, 0x00);
                machine.Poke(ObjectX + 1, 0x70);
                machine.Poke(ObjectY + 1, 0x70);
                break;
            case 3:
                machine.Poke(Type + 1, 0x26);
                machine.Poke(Column + 1, 20);
                machine.Poke(Row + 1, 15);
                machine.Poke(ObjectX + 1, 0x90);
                machine.Poke(ObjectY + 1, 0x80);

                // A caught enemy in a bubble elsewhere goes on being worked while the game waits.
                machine.Poke(Type + 5, 0x1A);
                machine.Poke(EnemyType + 5, 0x1A);
                machine.Poke(Variant + 5, 5);
                machine.Poke(Direction + 5, 1);
                machine.Poke(Column + 5, 10);
                machine.Poke(Row + 5, 10);
                machine.Poke(ObjectX + 5, 0x60);
                machine.Poke(ObjectY + 5, 0x60);
                break;
        }
    }

    // The port's tables loaded from the game's bytes before, and what the game's rolls were.
    private sealed class Rig
    {
        private readonly ObjectTable _objects = new();
        private readonly EntityTable _entities = new();

        internal Rig(Capture capture, byte spriteEnable)
        {
            RamImage.Load(capture.Before, _objects);
            RamImage.Load(capture.Before, _entities);
            _entities.SpriteEnable = spriteEnable;

            PlayerTable players = new();
            RamImage.Load(capture.Before, players);

            BbRandom random = new(new ScriptedRandomSource(capture.Timer.Select(b => (int)b)))
            {
                Low = capture.Before[Random],
                High = capture.Before[Random + 1],
            };

            Scores scores = new();
            WaterFlow water = new(_entities, random);
            FoodDrop food = new(_entities, scores, random);
            BubblePop pop = new(_objects, _entities, players, food, scores, water);

            Giant = new(_entities, _objects, pop, food, random)
            {
                Sweeps = capture.Before[Sweeps],
                Phase = capture.Before[Phase],
            };
        }

        internal GiantBubble Giant { get; }

        internal void AssertMatches(Capture capture, byte spriteEnable, string context)
        {
            string expectedObjects = RamImage.Objects(capture.After);
            string actualObjects = RamImage.Objects(_objects);

            Assert.True(expectedObjects == actualObjects, $"{context}: {RamImage.Difference(expectedObjects, actualObjects)}");

            string expected = RamImage.Entities(capture.After);
            string actual = RamImage.Entities(_entities);

            Assert.True(expected == actual, $"{context}: {RamImage.EntityDifference(expected, actual)}");
            Assert.Equal(spriteEnable, _entities.SpriteEnable);
        }
    }

    private sealed class Tally
    {
        internal int Moves { get; private set; }

        internal int Caught { get; private set; }

        internal int Free { get; private set; }

        internal int Exits { get; private set; }

        internal void Count(byte typeBefore, Capture capture)
        {
            Moves += typeBefore == 0x46 ? 1 : 0;
            Exits += typeBefore == 0x46 && capture.After[Type] == 0x38 ? 1 : 0;

            for (int slot = 2; slot < ObjectTable.Capacity; slot++)
            {
                Caught += capture.Before[Type + slot] != capture.After[Type + slot] && capture.After[Type + slot] == 0x38 ? 1 : 0;
            }

            for (int enemy = 2; enemy < EntityTable.Capacity; enemy++)
            {
                Free += capture.Before[0xB2 + enemy] is > 0 and < 0x0B && capture.After[0xB2 + enemy] == 0x0B ? 1 : 0;
            }
        }
    }
}
