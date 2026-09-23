// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// The spawn against the real thing.
//
// The bytes below were read out of VICE running rebb64, on 2026-09-23. An exec checkpoint on $3A89,
// the first instruction after the spawn loop and the row copy, stopped the machine and every array
// the spawn writes was read for slots 2 to 7.
//
// Levels 6 and 60 are an intervention. The game was started on level 1 and stopped at $39B0, the
// `ldx SUBFLG` that picks the spawn list, and SUBFLG was written there. So the spawn read level 6's
// or level 60's list, and nothing else about those levels is in these bytes. They are here for
// classes 6 and 7, which read past the ends of the spawn tables.
//
// The turn timers come from the RNG, which the port cannot match - see BbRandom. The capture's
// values are checked to lie in the range the table and the five random bits allow.
[Trait("Level", "Integration")]
public sealed class EnemySpawnerGoldenTests
{
    private const int EnemyBase = 2;

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    // Slot by slot from slot 2: $B2, $BA, $C2, $8520, $8548, $8598, $85E8, $8638, $8750, $8778, and
    // then $8660, which is the random one. $8688 read the same as $8660 in every slot.
    private static readonly Dictionary<int, byte[][]> s_captures = new()
    {
        [1] =
        [
            [0x02, 0x8C, 0x15, 0x04, 0x0C, 0x73, 0x01, 0x14, 0x04, 0x03, 0x22],
            [0x02, 0x8C, 0x15, 0x04, 0x0C, 0x73, 0x01, 0x22, 0x04, 0x03, 0x35],
            [0x02, 0x8C, 0x15, 0x04, 0x0C, 0x73, 0x01, 0x30, 0x04, 0x03, 0x2F],
        ],
        [6] =
        [
            [0x02, 0xD4, 0x3D, 0x04, 0x0C, 0x73, 0x01, 0x24, 0x04, 0x03, 0x22],
            [0x08, 0xB4, 0x65, 0x02, 0x03, 0xB3, 0x01, 0x2C, 0x02, 0x01, 0x21],
            [0x08, 0x8C, 0x8D, 0x00, 0x03, 0xB3, 0x02, 0x34, 0x02, 0x01, 0x1B],
            [0x02, 0xE4, 0x8D, 0x04, 0x0C, 0x73, 0x01, 0x34, 0x04, 0x03, 0x39],
        ],
        [60] =
        [
            [0x09, 0xAC, 0x35, 0x00, 0x0F, 0xBB, 0x01, 0x28, 0x00, 0xFF, 0x18],
            [0x09, 0xC4, 0x35, 0x00, 0x0F, 0xBB, 0x01, 0x28, 0x00, 0xFF, 0x2B],
            [0x09, 0xDC, 0x35, 0x00, 0x0F, 0xBB, 0x01, 0x28, 0x00, 0xFF, 0x25],
            [0x09, 0xF4, 0x35, 0x00, 0x0F, 0xBB, 0x01, 0x28, 0x00, 0xFF, 0x2F],
        ],
    };

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(60)]
    public void SpawnsExactlyAsTheGameDoes(int number)
    {
        EntityTable entities = new();

        // A zero draw leaves each turn timer at its table's value, the bottom of its range.
        EnemySpawner spawner = new(entities, new(new FakeRandomSource { RandomValue = 0 }));

        spawner.Spawn(s_levels.Level(number).Enemies);

        byte[][] capture = s_captures[number];
        Assert.Equal(capture.Length, entities.EnemyCount);

        for (int i = 0; i < capture.Length; i++)
        {
            int slot = EnemyBase + i;
            byte[] expected = capture[i];

            byte[] actual =
            [
                entities.State[slot],
                entities.X[slot],
                entities.Y[slot],
                entities.Frame[slot],
                entities.Colour[slot],
                entities.SpriteBase[slot],
                entities.Heading[slot],
                entities.HoldTimer[slot],
                entities.FrameCount[slot],
                entities.FrameMask[slot],
            ];

            Assert.Equal(expected[..^1], actual);
            Assert.InRange(expected[^1] - entities.TurnTimer[slot], 0, 0x1F);
        }
    }
}
