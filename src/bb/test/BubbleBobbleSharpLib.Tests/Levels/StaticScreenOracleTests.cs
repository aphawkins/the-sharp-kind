// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The level's static screen as the game draws it, against the real game run on SharpKind.Emulation.
//
// The game is played from the title and taken to each level with `EnterLevel`, to the level's first pass, with no other
// intervention. The screen the VIC shows (`$D018`) is read off: every cell of the 32 by 25 playfield, and the level's two
// multicolour registers. The port's `PlayfieldModel` has to name the same character in every cell (it calls the level's tile
// `$15` where the screen has `$10`) and the level the same colours.
//
// This stands in for a comparison with VICE screenshots: the playfield is characters and colour RAM, so the screen codes
// the game wrote say everything a screenshot would, bar the artwork, which `PlayfieldView8BitTests` proves per sheet.
[Trait("Level", "Integration")]
public sealed class StaticScreenOracleTests
{
    private const int ScreenColumns = 40;
    private const int GameTile = 0x10;
    private const int Space = 0x20;
    private const int FirstEdge = 0x0A;
    private const int WallColumns = 2;
    private const int LastWall = 30;
    private const int IntroText = 0xC0;
    private const int MemoryPointer = 0x18;
    private const int BankBase = 0x4000;
    private const int ScreenUnit = 0x400;
    private const int Multicolour1 = 0x22;
    private const int Multicolour2 = 0x23;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));

    public static TheoryData<int> Levels() => [1, 30, 100];

    [Theory]
    [MemberData(nameof(Levels))]
    public void ShowsTheSameCharactersAndColoursAsTheGame(int number)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();

        if (number != 1)
        {
            machine.EnterLevel(number);
        }

        PlayfieldModel field = Playfield.Build(s_levels.Level(number));
        int screen = BankBase + (((machine.PeekVic(MemoryPointer) >> 4) & 0x0F) * ScreenUnit);
        int compared = 0;

        for (int row = 0; row < PlayfieldModel.Rows; row++)
        {
            for (int column = 0; column < PlayfieldModel.Columns; column++)
            {
                byte shown = machine.Peek(screen + (row * ScreenColumns) + column);

                // The walls, the level's number and the ROUND and READY!! text of the level's start are the sidebar's characters.
                if (shown is not (Space or (>= FirstEdge and <= GameTile)))
                {
                    bool wall = column is < WallColumns or >= LastWall || shown >= IntroText;

                    Assert.True(wall, $"level {number}, row {row}, column {column}: ${shown:X2}");
                    continue;
                }

                byte expected = shown == GameTile ? PlayfieldModel.LevelTile : shown;

                Assert.True(
                    field.Character(column, row) == expected,
                    $"level {number}, row {row}, column {column}: the game shows ${shown:X2}, the port ${field.Character(column, row):X2}");
                compared++;
            }
        }

        Assert.True(compared > PlayfieldModel.Rows * (PlayfieldModel.Columns - (2 * WallColumns)) / 2, "too few cells were compared");
        Assert.Equal(field.Colours >> 4, machine.PeekVic(Multicolour1) & 0x0F);
        Assert.Equal(field.Colours & 0x0F, machine.PeekVic(Multicolour2) & 0x0F);
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
