// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// Class 0 against the real thing, a frame at a time.
//
// The two files in Captures were read out of VICE running rebb64, on 2026-09-23, with one player. Two
// exec checkpoints bracket $1E6C for the enemy slots: $1D21, the `jsr D_1E6C` itself, and $1D24,
// where it returns to. Every enemy array $EA08 reads or writes was read at both.
//
//   * walker-level-1.txt is 900 calls on level 1, across slots 2, 3 and 4: 255 frames of jumping,
//     212 of falling and 433 of walking. Level 1 has no gap an enemy can leap.
//   * walker-level-3.txt is 1,200 calls on level 3, across slots 2 to 5, with six leaps started and
//     278 frames of leaping. Level 3 is an intervention: the game stopped at $09A5, the level's
//     initialisation, and SUBFLG was written there as 2. It counts from zero.
//
// The player was driven only by pressing up every few seconds, so they jumped between floors and the
// enemies saw them above, level and below.
//
// Each line is one call: the slot, then the bytes before, then the bytes after. The bytes are the two
// players' $B2 and $C2, then the slot's $B2, $BA, $C2, $8520, $8610, $85E8, $87A0, $87C8, $87F0,
// $8840, $8868, $8660, $8688, $86B0, $86D8, $8700, $8750 and $8778.
[Trait("Level", "Integration")]
public sealed class EnemyWalkerGoldenTests
{
    private const int Fields = 22;

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "zones.json"));

    public static TheoryData<int, int> Lines()
    {
        TheoryData<int, int> lines = [];

        foreach (int level in (int[])[1, 3])
        {
            for (int i = 0; i < Capture(level).Count; i++)
            {
                lines.Add(level, i);
            }
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(Lines))]
    public void StepsExactlyAsTheGameDoes(int level, int line)
    {
        (int slot, byte[] before, byte[] after) = Capture(level)[line];
        EntityTable entities = new();
        Load(entities, slot, before);

        EnemyFrame frame = Frame(entities);
        frame.Step(slot, 0, Map(level));

        Assert.Equal(Hex(after), Hex(Save(entities, slot)));
    }

    [Fact]
    public void CoversEveryWayOutOfTheWalker()
    {
        List<(int Slot, byte[] Before, byte[] After)> capture = [.. Capture(1), .. Capture(3)];

        // Rising, winding up, falling from a jump, falling off a ledge, and a leap started.
        Assert.Contains(capture, c => c.Before[10] is >= 0x10 and < 0x80);
        Assert.Contains(capture, c => c.Before[10] is > 0x00 and < 0x10);
        Assert.Contains(capture, c => c.Before[10] == 0x00);
        Assert.Contains(capture, c => c.Before[12] < 0x80);
        Assert.Contains(capture, c => c.Before[10] == 0xFF && c.After[10] == 0x0E);
        Assert.Contains(capture, c => c.Before[10] == 0xFF && c.After[10] == 0x1E);
    }

    private static EnemyFrame Frame(EntityTable entities)
        => TestEnemies.Frame(entities, new(), new(new FakeRandomSource()));

    private static SolidMap Map(int level) => SolidMap.Build(s_levels.Level(level), s_zones.Zones(level));

    private static List<(int Slot, byte[] Before, byte[] After)> Capture(int level)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Enemies", "Captures", $"walker-level-{level}.txt");

        return [.. File.ReadAllLines(path).Select(line =>
        {
            string[] parts = line.Split(' ');
            return (int.Parse(parts[0], CultureInfo.InvariantCulture), Convert.FromHexString(parts[1]), Convert.FromHexString(parts[2]));
        })];
    }

    // A captured slot, with the players and the three arrays $1CDB tests set so that $1E6C runs.
    private static void Load(EntityTable entities, int slot, byte[] b)
    {
        Assert.Equal(Fields, b.Length);

        entities.Slot(0).State = b[0];
        entities.Slot(1).State = b[1];
        entities.Slot(0).Y = b[2];
        entities.Slot(1).Y = b[3];
        entities.Slot(slot).Mode = 0xFF;
        entities.Slot(slot).FlashTimer = 0;
        entities.Slot(slot).HoldTimer = 0;

        entities.Slot(slot).State = b[4];
        entities.Slot(slot).X = b[5];
        entities.Slot(slot).Y = b[6];
        entities.Slot(slot).Frame = b[7];
        entities.Slot(slot).AnimationTimer = b[8];
        entities.Slot(slot).Heading = b[9];
        entities.Slot(slot).RiseCounter = b[10];
        entities.Slot(slot).FallCounter = b[11];
        entities.Slot(slot).GroundState = b[12];
        entities.Slot(slot).LeftFlag = b[13];
        entities.Slot(slot).RightFlag = b[14];
        entities.Slot(slot).TurnTimer = b[15];
        entities.Slot(slot).TurnInterval = b[16];
        entities.Slot(slot).ClimbNext = ClimbNext(slot, b[17]);
        entities.Slot(slot).LeapFlag = b[18];
        entities.Slot(slot).ClimbFlag = b[19];
        entities.Slot(slot).FrameCount = b[20];
        entities.Slot(slot).FrameMask = b[21];
    }

    private static byte[] Save(EntityTable entities, int slot) => [
        entities.Slot(0).State, entities.Slot(1).State, entities.Slot(0).Y, entities.Slot(1).Y,
        entities.Slot(slot).State, entities.Slot(slot).X, entities.Slot(slot).Y, entities.Slot(slot).Frame,
        entities.Slot(slot).AnimationTimer, entities.Slot(slot).Heading, entities.Slot(slot).RiseCounter,
        entities.Slot(slot).FallCounter, entities.Slot(slot).GroundState, entities.Slot(slot).LeftFlag,
        entities.Slot(slot).RightFlag, entities.Slot(slot).TurnTimer, entities.Slot(slot).TurnInterval,
        (byte)(slot + (entities.Slot(slot).ClimbNext ? 0x28 : 0x00)), entities.Slot(slot).LeapFlag,
        entities.Slot(slot).ClimbFlag, entities.Slot(slot).FrameCount, entities.Slot(slot).FrameMask,
    ];

    // $86B0 holds the slot, or the slot plus $28. Nothing else is a value it can hold.
    private static bool ClimbNext(int slot, byte raw)
    {
        Assert.True(raw == slot || raw == slot + 0x28, $"$86B0 is {raw:X2} for slot {slot}");
        return raw != slot;
    }

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes);
}
