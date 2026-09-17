// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharp.Renditions.EightBit;
using SharpKind;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// The HUD is eight rows of text at fixed places, so the recording surface proves it outright: what
// is drawn, where it lands and which colour it wears. The rows are the ones the pointers in $E3A7
// and the arguments in $046C work out to, and the colours are the ones $AB93 leaves in colour RAM.
public sealed class HudView8BitTests
{
    // Three labels, three scores and a row of lives each.
    private const int ExpectedRows = 8;

    private const int Cell = 8;

    // The character the view draws a life with - see LIFE_ICON in tools/bb/export-csharp-assets.py.
    private const char LifeMarker = '_';

    // Every row of the HUD starts at screen column 33; the labels are indented two further.
    private const float Column = 33 * Cell;
    private const float LabelColumn = 35 * Cell;

    [Fact]
    public void DrawsALabelAScoreAndARowOfLivesForEachPlayer()
    {
        RecordingGraphics graphics = Draw(Hud(3, 3));

        Assert.Equal(ExpectedRows, graphics.LeftTexts.Count);
        Assert.All(graphics.LeftTexts, x => Assert.Equal("Small", x.FontType));
    }

    // $AB93 writes each label a row above the score it names, at column 35, all three in colour 7.
    [Fact]
    public void PutsTheLabelsAboveTheScoresTheyName()
    {
        RecordingGraphics graphics = Draw(Hud(3, 3));

        Assert.Equal(
            [("1UP", new Vector2(LabelColumn, 4 * Cell)),
             ("2UP", new Vector2(LabelColumn, 11 * Cell)),
             ("TOP", new Vector2(LabelColumn, 18 * Cell))],
            graphics.LeftTexts
                .Where(x => x.Colour == TestSurface.Colour(7))
                .Select(x => (x.Text, x.Position)));
    }

    // $50E9, $5201 and $5341 on a forty-column screen from $5000: rows 5, 12 and 20, all at column 33.
    [Fact]
    public void PutsEachScoreWhereItsPointerLands()
    {
        RecordingGraphics graphics = Draw(
            new([0x00, 0x11, 0x11], [0x00, 0x22, 0x22], [0x00, 0x33, 0x33], 3, 3));

        Assert.Equal(
            [("  1111", new Vector2(Column, 5 * Cell), TestSurface.Colour(5)),
             ("  2222", new Vector2(Column, 12 * Cell), TestSurface.Colour(3)),
             ("  3333", new Vector2(Column, 20 * Cell), TestSurface.Colour(1))],
            graphics.LeftTexts
                .Where(x => x.Text.Length == HudModel.ScoreDigits)
                .Select(x => (x.Text, x.Position, x.Colour)));
    }

    // $5139 and $5251: rows 7 and 14, at the same column the scores start at. $046C fills the row
    // from its right-hand end, so three lives in a row of seven leave the first four cells empty -
    // which here means the text starts four columns along rather than carrying spaces.
    [Theory]
    [InlineData(1, 6)]
    [InlineData(3, 4)]
    [InlineData(7, 0)]
    public void FillsTheLivesRowFromItsRightHandEnd(int lives, int empty)
    {
        RecordingGraphics graphics = Draw(Hud(lives, lives));

        Assert.Equal(
            [(lives, new Vector2(Column + (empty * Cell), 7 * Cell), TestSurface.Colour(5)),
             (lives, new Vector2(Column + (empty * Cell), 14 * Cell), TestSurface.Colour(3))],
            Markers(graphics));
    }

    // A count larger than the row holds draws no more than the row holds.
    [Fact]
    public void DrawsNoMoreMarkersThanTheRowHolds()
    {
        RecordingGraphics graphics = Draw(Hud(9, 9));

        Assert.All(Markers(graphics), x => Assert.Equal(HudModel.LifeCells, x.Length));
    }

    // $046C returns without writing anything when the count is negative, so a player who is out has
    // no row of lives at all - while their label and score stay on the screen.
    [Fact]
    public void DrawsNoLivesForAPlayerWhoIsOut()
    {
        RecordingGraphics graphics = Draw(Hud(3, -1));

        // Player one's row survives, and it is the only one left.
        Assert.Equal(
            [(3, new Vector2(Column + (4 * Cell), 7 * Cell), TestSurface.Colour(5))],
            Markers(graphics));

        // The label and the score are not the lives, and both are still drawn.
        Assert.Equal(ExpectedRows - 1, graphics.LeftTexts.Count);
        Assert.Contains(graphics.LeftTexts, x => x.Text == "2UP");
    }

    // The HUD is drawn in hires rather than multicolour: none of the four colour bytes $AB93 writes
    // has bit 3 set, so nothing here is repainted and no sheet is touched.
    [Fact]
    public void DrawsNothingFromASheet()
    {
        RecordingGraphics graphics = Draw(Hud(3, 3));

        Assert.Empty(graphics.ImageParts);
        Assert.Empty(graphics.Images);
    }

    // The rows of life markers, which are the only ones drawn out of the sheet's last cell - the one
    // the export puts screen code $1D in, and which nothing else in the HUD uses.
    private static IEnumerable<(int Length, Vector2 Position, FastColor Colour)> Markers(
        RecordingGraphics graphics)
        => graphics.LeftTexts
            .Where(x => x.Text.All(c => c == LifeMarker))
            .Select(x => (x.Text.Length, x.Position, x.Colour));

    private static HudModel Hud(int playerOneLives, int playerTwoLives) => new(
        new byte[HudModel.ScoreBytes],
        new byte[HudModel.ScoreBytes],
        new byte[HudModel.ScoreBytes],
        playerOneLives,
        playerTwoLives);

    private static RecordingGraphics Draw(HudModel model)
    {
        RecordingGraphics graphics = new(320, 200);

        new EightBitRendition().CreateHudView(new TestSurface(graphics)).Draw(model);

        return graphics;
    }
}
