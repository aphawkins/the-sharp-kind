// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The port's map of every level against the game's, cell by cell, as the game holds it at the level's first pass:
// the 25 rows of 32 columns at `$8500` (a row is 40 bytes), each cell's solid bit and its two bubble-current bits.
//
// The game builds the map in steps: each row is filled with the bubble current in the level's right-hand wall byte
// (`$E1D1`); the level's commands then run in order (`$E1F3`), each rectangle overwriting whole bytes (`$E239`) and each
// mirror copying the left half onto the right with a current to the left turned to the right (`$E24C`); and last
// `$3998` sets the solid bit of every cell whose rendered screen character is $10 to $1D. The port does all but the last,
// taking the solid bits from the level's bitmap, which agrees wherever it has been compared.
//
// It does not agree for the levels in `s_gaps` (work-items/bb-solidmap-fidelity.md), which the test leaves out.
[Trait("Level", "Integration")]
public sealed class SolidMapOracleTests
{
    private const int MapBase = 0x8500;
    private const int RowStride = 40;
    private const int Mask = 0x83;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Asset("zones.json"));

    // Levels 72 to 87 hold their first pass with the platforms not yet in the game's map (level 76's platforms are drawn
    // in characters $C0 upwards, which `$3998` does not count), and 36, 48, 52 and 70 differ by a few cells' current.
    private static readonly HashSet<int> s_gaps = [36, 48, 52, 70, .. Enumerable.Range(72, 16)];

    [Fact]
    public void EveryLevelsMapIsTheGames()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();

        List<string> wrong = [];

        for (int number = 1; number < 100; number++)
        {
            if (number > 1)
            {
                machine.EnterLevel(number);
            }

            if (s_gaps.Contains(number))
            {
                continue;
            }

            List<string> cells = Mismatches(machine, SolidMap.Build(s_levels.Level(number), s_zones.Zones(number)));

            if (cells.Count > 0)
            {
                int solid = cells.Count(x => x.EndsWith(" S", StringComparison.Ordinal));

                wrong.Add($"level {number}: {cells.Count} cells, {solid} solid, first {string.Join("; ", cells.Take(6))}");
            }
        }

        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong));
    }

    private static List<string> Mismatches(Rebb64Machine machine, SolidMap map)
    {
        List<string> cells = [];

        for (int row = 0; row < SolidMap.Rows; row++)
        {
            for (int column = 0; column < SolidMap.Columns; column++)
            {
                int game = machine.Peek(MapBase + (row * RowStride) + column) & Mask;
                int port = (map[row, column] ? 0x80 : 0) | map.Direction(row, column);

                if (game != port)
                {
                    string solid = (game ^ port) >= 0x80 ? " S" : string.Empty;

                    cells.Add($"({row},{column}) game {game:X2} port {port:X2}{solid}");
                }
            }
        }

        return cells;
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
