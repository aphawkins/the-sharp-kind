// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Enemies;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The giant bonus a cleared level drops when a special item has set `$69`, against the real game run on SharpKind.Emulation.
//
// **An item's effect is called directly** (`Invoke`, an intervention) some frames into level 1, with the enemies left
// alone but the level made clear by writing every enemy slot's state and `$4A` zero: the game then clears the level itself.
// The drop (`$1750` to `$175F`) is bracketed, and the port's `SuperBonus.Begin` runs on the bytes before. Then each turn of
// slot 2 in state `$15` is bracketed (`$1CDB` to `$1D24`) and worked by the port; on some turns player 1 is put on the block
// (an intervention) so that the score and the emptying of the slots are compared too.
[Trait("Level", "Integration")]
public sealed class SuperBonusOracleTests
{
    private const int Effect = 0x348A;
    private const int Colour = 0x2AE9;
    private const int Arg = 0x69;
    private const int ScoreBytes = 0x0400;

    private const int SlotStart = 0x1CDB;
    private const int SlotEnd = 0x1D24;

    [Theory]
    [InlineData(0x348A)]
    [InlineData(0x3495)]
    [InlineData(0x34FA)]
    public void DropsTheBlockExactlyAsTheGameDoes(int effect)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = Cleared(effect);

        Capture capture = machine.Bracket(0x1750, 0x175F, 300_000);

        EntityTable entities = new();
        RamImage.Load(capture.Before, entities);

        new SuperBonus(entities, new Scores(), new(new(), entities, new(), Items(entities), new(new ScriptedRandomSource([]))))
            .Begin();

        string expected = RamImage.Entities(capture.After);
        string actual = RamImage.Entities(entities);

        Assert.True(expected == actual, RamImage.EntityDifference(expected, actual));
        Assert.Equal(0x15, entities.State[2]);
    }

    [Fact]
    public void FallsAndIsTakenExactlyAsTheGameDoes()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        SolidMap map = SolidMap.Build(Levels().Level(1), Zones().Zones(1));
        Rebb64Machine machine = Cleared(Effect);
        int turns = 0;
        int taken = 0;

        for (int turn = 0; turn < 200 && machine.Peek(0xB2 + 2) != 0x15; turn++)
        {
            machine.RunFrames(2);
        }

        Assert.Equal(0x15, machine.Peek(0xB2 + 2));

        for (int turn = 0; turn < 120 && machine.Peek(0xB2 + 2) == 0x15; turn++)
        {
            Capture capture = machine.Bracket(
                SlotStart,
                SlotEnd,
                300_000,
                m => Take(m, turn),
                m => m.Cpu.X == 2);

            EntityTable entities = new();
            RamImage.Load(capture.Before, entities);
            Scores scores = new();

            for (int i = 0; i < 6; i++)
            {
                scores.Add(i, capture.Before[ScoreBytes + i]);
            }

            LevelClear clear = new(new(), entities, new(), Items(entities), new(new ScriptedRandomSource([])))
            {
                BonusArg = capture.Before[Arg],
                BonusColour = capture.Before[Colour],
            };

            BbRandom random = new(new ScriptedRandomSource([]));
            EnemyFrame frame = TestEnemies.Frame(entities, new(), random, scores, new(new()), new(entities, random));
            frame.SuperBonus = new(entities, scores, clear);
            frame.Step(2, capture.Before[0x08], map);

            string expected = RamImage.Entities(capture.After);
            string actual = RamImage.Entities(entities);

            Assert.True(expected == actual, $"turn {turn}: {RamImage.EntityDifference(expected, actual)}");

            for (int i = 0; i < 6; i++)
            {
                Assert.Equal(capture.After[ScoreBytes + i], scores.Bytes[i]);
            }

            turns++;
            taken += capture.After[0xB2 + 2] == 0 ? 1 : 0;
        }

        Assert.True(turns > 30 && taken == 1, $"{turns} turns, {taken} taken");
    }

    // The effect made a bonus due, and the level made empty: the game clears it on its next pass.
    private static Rebb64Machine Cleared(int effect)
    {
        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(150);

        byte music = machine.Peek(0x05AD);
        machine.Poke(0x05AD, 0x60);
        machine.Invoke(effect, 0, 0, 100_000);
        machine.Poke(0x05AD, music);

        for (int slot = 2; slot < 8; slot++)
        {
            machine.Poke(0xB2 + slot, 0);
        }

        machine.Poke(0x4A, 0);

        return machine;
    }

    // On turn 40, player 1 is put on the block: an intervention.
    private static void Take(Rebb64Machine machine, int turn)
    {
        if (turn != 40)
        {
            return;
        }

        machine.Poke(0xBA, (byte)(machine.Peek(0xBC) + 8));
        machine.Poke(0xC2, (byte)(machine.Peek(0xC4) + 4));
    }

    private static LevelStore Levels() => LevelStore.Read(Asset("levels.json"));

    private static ZoneStore Zones() => ZoneStore.Read(Asset("zones.json"));

    private static LevelItems Items(EntityTable entities)
        => new(entities, new Scores(), new ItemEffects(new PlayerTable(), entities), new(new ScriptedRandomSource([])));

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
