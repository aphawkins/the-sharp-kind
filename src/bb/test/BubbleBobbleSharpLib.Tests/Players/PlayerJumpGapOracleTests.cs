// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;
using BubbleBobbleSharpLib.Tests.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// A jump that ends with nothing under the player, against the real game run on SharpKind.Emulation.
//
// **An intervention puts the player there.** Level 1 has no gap wide enough to jump across, so at its first pass player 1
// is put in mid-air above open cells at the end of a fall: `$87A0` and `$87C8` `$FF`/`$0C` (a jump's last turns), `$87F0`
// left at `$FF` as a jump leaves it. The landing check at the end of the fall finds no floor, which is where `$87F0` goes
// from `$FF` to zero and the descent takes over. Every turn of the slot, `$1CDB` to `$1D24` with X 0, is then compared
// with `PlayerFrame.Slot` until the player is on a floor again.
[Trait("Level", "Integration")]
public sealed class PlayerJumpGapOracleTests
{
    private const int SlotStart = 0x1CDB;
    private const int SlotEnd = 0x1D24;

    private const int PlayerX = 0xBA;
    private const int PlayerY = 0xC2;
    private const int Rise = 0x87A0;
    private const int Fall = 0x87C8;
    private const int Ground = 0x87F0;

    private const byte FallingAt = 0x0C;
    private const int OpenRows = 5;
    private const int Turns = 40;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Asset("zones.json"));

    public static TheoryData<int> Spots() => [0, 1, 2];

    [Theory]
    [MemberData(nameof(Spots))]
    public void FallsThroughTheGapExactlyAsTheGameDoes(int spot)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        SolidMap map = SolidMap.Build(s_levels.Level(1), s_zones.Zones(1));
        (int column, int row) = OpenSpots(map)[spot];

        Rebb64Machine machine = new();
        machine.StartOnePlayer();

        machine.Poke(PlayerX, (byte)((column << 3) + 0x14));
        machine.Poke(PlayerY, (byte)((row << 3) + 0x2D));
        machine.Poke(Rise, 0xFF);
        machine.Poke(Fall, FallingAt);
        machine.Poke(Ground, 0xFF);

        bool left = false;

        for (int turn = 0; turn < Turns; turn++)
        {
            Capture capture = machine.Bracket(SlotStart, SlotEnd, 300_000, when: m => m.Cpu.X == 0);

            EntityTable entities = new();
            RamImage.Load(capture.Before, entities);
            Frame(entities).Slot(0, capture.Before[0x85E8], capture.Before[0x08], map);

            string expected = RamImage.Entities(capture.After);
            string actual = RamImage.Entities(entities);

            Assert.True(expected == actual, $"turn {turn}: {RamImage.EntityDifference(expected, actual)}");

            left |= capture.Before[Ground] == 0xFF && capture.After[Ground] != 0xFF;
        }

        Assert.True(left, "the player never left the ground state, so the fall was not compared");
    }

    // Cells with open air for the rows below, away from the walls, from the top of the level down.
    private static List<(int Column, int Row)> OpenSpots(SolidMap map)
    {
        List<(int Column, int Row)> spots = [];

        for (int row = 3; row < SolidMap.Rows - OpenRows - 2; row++)
        {
            for (int column = 3; column < SolidMap.Columns - 3; column++)
            {
                bool open = true;

                for (int below = 0; below < OpenRows && open; below++)
                {
                    open = !map[row + below, column] && !map[row + below, column + 1] && !map[row + below, column - 1];
                }

                if (open)
                {
                    spots.Add((column, row));
                }
            }
        }

        return [spots[0], spots[spots.Count / 2], spots[^1]];
    }

    private static PlayerFrame Frame(EntityTable entities)
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
            new WaterFlow(entities, new BbRandom(new ScriptedRandomSource([]))));
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
