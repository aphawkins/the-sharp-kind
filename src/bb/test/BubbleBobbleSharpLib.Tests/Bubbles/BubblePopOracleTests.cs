// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;
using BubbleBobbleSharpLib.Tests.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Bubbles;

// The pop's special-bubble arms (`$3E02`) against the real game run on SharpKind.Emulation.
//
// **Every scene is an intervention.** At the entry of `$E90E`, in a level 1 pass, the eighteen object slots are freed and
// one is written in as a touched (`$34`) or expired (`$3A`) bubble holding the type under test, on a chosen cell, with a
// chosen player (`$0193`) and, for the bolt, a chosen player frame. `$E90E` is then bracketed to `$0A4B` with interrupts off
// and the port's `BubblePop.Update` is run on the same bytes. Compared: every object and entity byte, the water (`$4D`
// and its cells), the EXTEND letters, the chain counts, the scores, the random state, and the pass's counter (`$597F`).
[Trait("Level", "Integration")]
public sealed class BubblePopOracleTests
{
    private const int Slot = 5;
    private const int Water = 0x4D;
    private const int Columns = 0xA74B;
    private const int Rows = 0xA756;
    private const int Cells = 11;
    private const int Letters = 0x54;
    private const int Chains = 0x46;
    private const int ScoreBytes = 0x0400;
    private const int Counter = 0x597F;
    private const int Random = 0x26;

    public static TheoryData<byte, byte, byte, byte, byte, byte> Scenes() => new()
    {
        // held, type, player, column, row, water
        { 0x06, 0x34, 0, 10, 10, 0 },
        { 0x06, 0x3A, 1, 20, 7, 0 },
        { 0x06, 0x34, 0, 10, 0x1C, 0 },
        { 0x06, 0x34, 1, 10, 0x1B, 0 },
        { 0x06, 0x34, 0, 10, 10, 4 },
        { 0x06, 0x34, 0, 10, 10, 0xFF },
        { 0x08, 0x34, 0, 12, 9, 0 },
        { 0x08, 0x3A, 1, 12, 9, 0 },
        { 0x0A, 0x34, 0, 12, 9, 0 },
        { 0x0A, 0x34, 1, 22, 15, 0 },
        { 0x0A, 0x3A, 0, 5, 20, 0 },
        { 0x0C, 0x34, 0, 12, 9, 0 },
        { 0x04, 0x34, 1, 12, 9, 0 },
    };

    [Theory]
    [MemberData(nameof(Scenes))]
    public void PopsExactlyAsTheGameDoes(byte held, byte type, byte player, byte column, byte row, byte water)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        foreach (byte frame in (byte[])[0, 4, 9, 10])
        {
            Rebb64Machine machine = new();
            machine.StartOnePlayer();

            Capture capture = machine.Bracket(
                0xE90E,
                0x0A4B,
                300_000,
                m => Place(m, held, type, player, column, row, water, frame));

            ObjectTable objects = new();
            EntityTable entities = new();
            PlayerTable players = new();
            Scores scores = new();
            BbRandom random = new(new ScriptedRandomSource(capture.Timer.Select(b => (int)b)))
            {
                Low = capture.Before[Random],
                High = capture.Before[Random + 1],
            };

            RamImage.Load(capture.Before, objects);
            RamImage.Load(capture.Before, entities);
            WaterFlow flow = new(entities, random) { Mask = capture.Before[Water] };
            capture.Before.AsSpan(Columns, Cells).CopyTo(flow.Columns);
            capture.Before.AsSpan(Rows, Cells).CopyTo(flow.Rows);

            BubblePop pop = new(objects, entities, players, new(entities, scores, random), scores, flow)
            {
                Counter = capture.Before[Counter],
            };

            for (int p = 0; p < PlayerTable.Capacity; p++)
            {
                players.ExtendLetters[p] = capture.Before[Letters + p];
                pop.Chain[p] = capture.Before[Chains + p];
            }

            for (int i = 0; i < 6; i++)
            {
                scores.Add(i, capture.Before[ScoreBytes + i]);
            }

            pop.Update();

            string context = $"held ${held:X2} type ${type:X2} player {player} at {column},{row} water ${water:X2} frame {frame}";
            string expected = RamImage.Objects(capture.After);
            string actual = RamImage.Objects(objects);

            Assert.True(expected == actual, $"{context}: {RamImage.Difference(expected, actual)}");

            expected = RamImage.Entities(capture.After);
            actual = RamImage.Entities(entities);

            Assert.True(expected == actual, $"{context}: {RamImage.EntityDifference(expected, actual)}");
            Assert.Equal(capture.After[Water], flow.Mask);
            Assert.Equal(Convert.ToHexString(capture.After, Columns, Cells), Convert.ToHexString(flow.Columns.ToArray()));
            Assert.Equal(Convert.ToHexString(capture.After, Rows, Cells), Convert.ToHexString(flow.Rows.ToArray()));
            Assert.Equal(capture.After[Letters], players.ExtendLetters[0]);
            Assert.Equal(capture.After[Letters + 1], players.ExtendLetters[1]);
            Assert.Equal(Convert.ToHexString(capture.After, ScoreBytes, 6), Convert.ToHexString(scores.Bytes.ToArray()));
            Assert.Equal(capture.After[Random], random.Low);
            Assert.Equal(capture.After[Counter], pop.Counter);
        }
    }

    private static void Place(Rebb64Machine machine, byte held, byte type, byte player, byte column, byte row, byte water, byte frame)
    {
        for (int slot = 0; slot < ObjectTable.Capacity; slot++)
        {
            machine.Poke(0xCA + slot, ObjectTable.FreeType);
        }

        machine.Poke(0xCA + Slot, type);
        machine.Poke(0xAA42 + Slot, held);
        machine.Poke(0x0193 + Slot, player);
        machine.Poke(0xA9B2 + Slot, 0);
        machine.Poke(0xA9C4 + Slot, 1);
        machine.Poke(0xA9D6 + Slot, 2);
        machine.Poke(0xAA30 + Slot, 0);
        machine.Poke(0xDC + Slot, column);
        machine.Poke(0xEE + Slot, row);
        machine.Poke(0xAA0C + Slot, (byte)((column << 3) + 0x14));
        machine.Poke(0xAA1E + Slot, (byte)((row << 3) + 0x15));
        machine.Poke(Water, water);
        machine.Poke(0x8520 + player, frame);
    }
}
