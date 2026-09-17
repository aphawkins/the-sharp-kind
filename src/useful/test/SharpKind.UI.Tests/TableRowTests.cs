// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Graphics.Fakes;

namespace SharpKind.UI.Tests;

// A cell is placed against a point rather than laid out in a box, letting a units suffix share a column with the quantity it follows.
public class TableRowTests
{
    private static readonly FastColor s_text = new(0xFFFFFFFF);
    private static readonly FastColor s_highlight = new(0xFFFF0000);

    private static readonly ControlStyle s_style = new(
        "Small",
        ControlColors.TextOnly(s_text),
        new(s_highlight, s_text),
        ControlColors.TextOnly(s_text));

    [Fact]
    public void CellsAreDrawnAtTheirColumnsOffsetFromTheRow()
    {
        RecordingGraphics graphics = new();
        TableRow row = Row(graphics, [new(0, TextAlignment.Left), new(50, TextAlignment.Left)], "Food", "t");
        row.Position = new(10, 20);

        row.Draw();

        Assert.Equal(2, graphics.LeftTexts.Count);
        Assert.Equal(new(10, 20), graphics.LeftTexts[0].Position);
        Assert.Equal(new(60, 20), graphics.LeftTexts[1].Position);
    }

    [Fact]
    public void ARightAlignedCellEndsAtItsColumn()
    {
        RecordingGraphics graphics = new();
        TableRow row = Row(graphics, [new(80, TextAlignment.Right)], "3.6");

        row.Draw();

        Assert.Empty(graphics.LeftTexts);
        Assert.Equal(new(80, 0), Assert.Single(graphics.RightTexts).Position);
    }

    [Fact]
    public void ANormalRowDrawsNoBlock()
    {
        RecordingGraphics graphics = new();
        TableRow row = Row(graphics, [new(0, TextAlignment.Left)], "Food");

        row.Draw();

        Assert.Empty(graphics.FilledRectangles);
    }

    [Fact]
    public void ASelectedRowFillsItsWholeWidthOnce()
    {
        RecordingGraphics graphics = new();
        TableRow row = Row(graphics, [new(0, TextAlignment.Left), new(50, TextAlignment.Left)], "Food", "t");
        row.State = ControlState.Selected;

        row.Draw();

        (_, float width, _, FastColor colour) = Assert.Single(graphics.FilledRectangles);
        Assert.Equal(s_highlight, colour);
        Assert.Equal(100, width);
    }

    // An empty cell is how a row says a quantity has no unit after it, so it
    // must not leave a stray draw behind.
    [Fact]
    public void AnEmptyCellDrawsNothing()
    {
        RecordingGraphics graphics = new();
        TableRow row = Row(graphics, [new(0, TextAlignment.Left), new(50, TextAlignment.Left)], "Food", string.Empty);

        row.Draw();

        Assert.Single(graphics.LeftTexts);
    }

    // The binding is read at every draw, so a row cannot be left showing a
    // price the market has moved off.
    [Fact]
    public void CellsAreReadAtEveryDraw()
    {
        RecordingGraphics graphics = new();
        MutableCells cells = new() { Values = ["3.6"] };
        TableRow row = new(graphics, s_style, [new(0, TextAlignment.Left)], cells);

        row.Draw();
        cells.Values = ["9.9"];
        row.Draw();

        Assert.Equal("3.6", graphics.LeftTexts[0].Text);
        Assert.Equal("9.9", graphics.LeftTexts[1].Text);
    }

    // A binding that answers shorter than the columns leaves the last ones
    // empty rather than failing.
    [Fact]
    public void FewerCellsThanColumnsDrawsWhatThereIs()
    {
        RecordingGraphics graphics = new();
        TableRow row = Row(graphics, [new(0, TextAlignment.Left), new(50, TextAlignment.Left)], "Food");

        row.Draw();

        Assert.Single(graphics.LeftTexts);
    }

    private static TableRow Row(RecordingGraphics graphics, TableColumn[] columns, params string[] cells)
        => new(graphics, s_style, columns, new MutableCells { Values = cells }) { Width = 100, Height = 8 };

    private sealed class MutableCells : IRowCells
    {
        public IReadOnlyList<string> Values { get; set; } = [];

        public IReadOnlyList<string> Cells => Values;
    }
}
