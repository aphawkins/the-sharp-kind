// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The scroll from level 1 to level 2 ($37C9) against the real game run on SharpKind.Emulation. The level is ended as `$0A6E`
// ends it, with `$21` written 1 some frames into level 1 (an intervention), and the screen the game shows ($5000) is read each
// time a row is about to move ($38E9), and once more when the scroll is over.
//
// The old level's tiles are character $10 by then ($3960) and the new level's $15, so each row of the screen says which level
// it is. Every cell of the 28 columns inside the decoration is compared with what the port's two scrolled levels draw there,
// but for the old level's items ($20 up), which the port does not scroll.
[Trait("Level", "Integration")]
public sealed class LevelScrollOracleTests
{
    private const int Screen = 0x5000;
    private const int RowStride = 40;
    private const int LevelComplete = 0x21;
    private const int Split = 0x20;
    private const int EndLevel = 0x0A6E;
    private const int MoveRow = 0x38E9;
    private const int AfterScroll = 0x09D9;
    private const int FirstRaster = 50;
    private const int RasterRow = 8;
    private const byte OldTile = 0x10;
    private const int FirstColumn = 2;
    private const int EndColumn = 30;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));

    [Fact]
    public void EveryRowOfTheScrollIsTheGames()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(100);
        machine.RunTo(Rebb64Machine.Pass, 50_000_000);
        machine.Poke(LevelComplete, 1);
        machine.Cpu.PC = EndLevel;

        List<byte[]> screens = [];
        List<byte> splits = [];

        for (int step = 0; step <= LevelScroll.Steps; step++)
        {
            machine.RunTo(step == LevelScroll.Steps ? AfterScroll : MoveRow, 50_000_000);
            screens.Add(machine.Ram.Slice(Screen, PlayfieldModel.Rows * RowStride).ToArray());
            splits.Add(machine.Peek(Split));
        }

        LevelScroll scroll = new(Playfield.Build(s_levels.Level(1)), Sidebars.Select(s_levels.Level(1)), s_levels.Level(2));

        for (int rows = 0; rows <= LevelScroll.Steps; rows += 2)
        {
            AssertScreen(screens[rows], scroll, rows);

            // The split the frame after a row moves is the one set for it: it is at the first row of the new level's wall.
            if (rows is > 3 and < LevelScroll.Steps)
            {
                Assert.Equal(scroll.Split, Math.Max(0, (splits[rows - 1] - FirstRaster) / RasterRow));
            }

            scroll.Pass();
        }

        Assert.True(scroll.Finished);
    }

    private static void AssertScreen(byte[] screen, LevelScroll scroll, int rows)
    {
        PlayfieldModel outgoing = scroll.OutgoingField;
        PlayfieldModel incoming = scroll.IncomingField;

        for (int row = 0; row < PlayfieldModel.Rows; row++)
        {
            PlayfieldModel shown = row < scroll.Split ? outgoing : incoming;
            int sourceRow = shown.SourceRow(row);

            for (int column = FirstColumn; column < EndColumn; column++)
            {
                byte game = screen[(row * RowStride) + column];

                if (game > PlayfieldModel.Space)
                {
                    continue;
                }

                if (game is OldTile or 0x15)
                {
                    Assert.True(game == OldTile == (row < scroll.Split), $"{rows} rows: row {row} is the other level's");
                }

                byte expected = game == OldTile ? PlayfieldModel.LevelTile : game;

                Assert.True(
                    expected == shown.Character(column, sourceRow),
                    $"{rows} rows: row {row}, column {column} is ${game:X2}, the port ${shown.Character(column, sourceRow):X2}");
            }
        }
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
