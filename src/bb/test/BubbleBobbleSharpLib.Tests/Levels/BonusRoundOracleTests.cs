// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;
using BubbleBobbleSharpLib.Tests.Enemies;
using BubbleBobbleSharpLib.Tests.Oracle;
using BubbleBobbleSharpLib.Tests.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The bonus round of the special item of type 24 (`$34A0`), against the real game run on SharpKind.Emulation.
//
// **The routine is called directly** (`Invoke`, an intervention) some frames into level 1, when the enemies are out.
// The port's `BonusRound.Start` runs on the bytes before and its entity table, `$5A7F`, `$5ABF` and `$A813`/`$A814` are
// compared with what the game left. Then the game runs on, and each turn of a slot in state `$16` or `$17` (an enemy
// tumbling and waiting) or `$18` (a player in the round) is bracketed, `$1CDB` to `$1D24`, and worked by the port from the
// bytes before. Now and then a player is put on a waiting enemy (an intervention) so that the touch is compared.
[Trait("Level", "Integration")]
public sealed class BonusRoundOracleTests
{
    private const int Effect = 0x34A0;
    private const int Music = 0x05AD;
    private const int Respawn = 0x5A7F;
    private const int Release = 0x5ABF;
    private const int Saved = 0xA813;
    private const int Random = 0x26;

    private const int SlotStart = 0x1CDB;
    private const int SlotEnd = 0x1D24;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Asset("zones.json"));

    [Theory]
    [InlineData(20)]
    [InlineData(150)]
    [InlineData(400)]
    public void StartsTheRoundExactlyAsTheGameDoes(int frames)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(frames);

        Capture capture = Start(machine);

        EntityTable entities = new();
        RamImage.Load(capture.Before, entities);
        RoundState round = new();
        byte[] saved = new byte[PlayerTable.Capacity];

        new BonusRound(entities, round, saved).Start();

        string expected = RamImage.Entities(capture.After);
        string actual = RamImage.Entities(entities);

        Assert.True(expected == actual, RamImage.EntityDifference(expected, actual));
        Assert.Equal(capture.After[Respawn], round.Respawn);
        Assert.Equal(capture.After[Release], round.Release);
        Assert.Equal(capture.After[Saved], saved[0]);
        Assert.Equal(capture.After[Saved + 1], saved[1]);
    }

    [Fact]
    public void RunsTheRoundExactlyAsTheGameDoes()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        SolidMap map = SolidMap.Build(s_levels.Level(1), s_zones.Zones(1));
        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(150);
        Start(machine);

        Tally tally = new();
        byte[] sticks = [0xFF, 0xF7, 0xFF, 0xFB, 0xFE, 0xFF, 0xEF, 0xFF];

        for (int turn = 0; turn < 900; turn++)
        {
            int slot = 7 - (turn % 8);

            if (!InRound(machine, slot))
            {
                continue;
            }

            int stick = turn / 8;
            machine.Joystick = sticks[stick % sticks.Length];

            Capture capture = machine.Bracket(
                SlotStart,
                SlotEnd,
                300_000,
                m => Touch(m, slot, turn),
                m => m.Cpu.X == slot && InRound(m, slot));

            Compare(capture, slot, map, tally, $"turn {turn}, slot {slot}, joystick {machine.Joystick:X2}");
        }

        Assert.True(
            tally.Tumbles > 0 && tally.Waits > 0 && tally.Touches > 0 && tally.Players > 0,
            $"{tally.Tumbles} tumbles, {tally.Waits} waits, {tally.Touches} touches, {tally.Players} player turns");
    }

    // A slot's turn in the round: an enemy tumbling or waiting, or a player in state $18.
    private static bool InRound(Rebb64Machine machine, int slot)
        => slot < PlayerTable.Capacity
            ? machine.Peek(0xB2 + slot) == BonusRound.PlayerState
            : machine.Peek(0xB2 + slot) is 0x16 or 0x17;

    // The turn the port takes from the bytes before, against the bytes the game left.
    private static void Compare(Capture capture, int slot, SolidMap map, Tally tally, string context)
    {
        EntityTable entities = new();
        RamImage.Load(capture.Before, entities);

        BbRandom random = new(new ScriptedRandomSource(capture.Timer.Select(b => (int)b)))
        {
            Low = capture.Before[Random],
            High = capture.Before[Random + 1],
        };

        WaterFlow water = new(entities, random);
        byte before = capture.Before[0xB2 + slot];

        if (slot >= PlayerTable.Capacity)
        {
            TestEnemies.Frame(entities, new ObjectTable(), random, new Scores(), new(new()), water)
                .Step(slot, capture.Before[0x08], map);
        }
        else
        {
            PlayerTable table = new();
            RamImage.Load(capture.Before, table);

            Frame(entities, water, table).Slot(slot, capture.Before[0x85E8 + slot], capture.Before[0x08], map);
        }

        tally.Count(slot, before, capture.After[0xB2 + slot]);

        string expected = RamImage.Entities(capture.After);
        string actual = RamImage.Entities(entities);

        Assert.True(expected == actual, $"{context}: {RamImage.EntityDifference(expected, actual)}");
    }

    // The effect ends in `jsr $05AD`, which starts the song and waits on the IRQ that a call with interrupts off never gets:
    // the call is made an `rts` for the length of the effect, as an intervention.
    private static Capture Start(Rebb64Machine machine)
    {
        byte music = machine.Peek(Music);

        machine.Poke(Music, 0x60);

        Capture capture = machine.Invoke(Effect, 0, 0, 100_000);

        machine.Poke(Music, music);

        return capture;
    }

    // On some turns, player 1 is put on the enemy in the slot, if it is waiting: an intervention.
    private static void Touch(Rebb64Machine machine, int slot, int turn)
    {
        if (slot < 2 || turn is not (3 or 100 or 200 or 300 or 400) || machine.Peek(0xB2 + slot) != 0x17)
        {
            return;
        }

        machine.Poke(0xBA, machine.Peek(0xBA + slot));
        machine.Poke(0xC2, machine.Peek(0xC2 + slot));
    }

    private static PlayerFrame Frame(EntityTable entities, WaterFlow water, PlayerTable table)
    {
        BubbleBlow blow = new(table, entities, new ObjectTable(), TestBlow.NoRings());
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

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);

    private sealed class Tally
    {
        internal int Tumbles { get; private set; }

        internal int Waits { get; private set; }

        internal int Touches { get; private set; }

        internal int Players { get; private set; }

        internal void Count(int slot, byte before, byte after)
        {
            if (slot < PlayerTable.Capacity)
            {
                Players++;
                return;
            }

            Tumbles += before == 0x16 ? 1 : 0;
            Waits += before == 0x17 ? 1 : 0;
            Touches += after == FoodDrop.FlyingState ? 1 : 0;
        }
    }
}
