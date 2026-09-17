// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Graphics.Fakes;

namespace SharpKind.UI.Tests;

// A list shorter than its window never scrolls, letting a screen that used to draw a fixed number of rows keep drawing them where it did.
public class ListViewTests
{
    private static readonly FastColor s_text = new(0xFFFFFFFF);
    private static readonly FastColor s_highlight = new(0xFFFF0000);

    private static readonly ControlStyle s_style = new(
        "Small",
        ControlColors.TextOnly(s_text),
        new(s_highlight, s_text),
        ControlColors.TextOnly(s_text));

    [Fact]
    public void AnEmptyListHasItsCursorAtZero()
    {
        ListView<Label> list = new(new RecordingGraphics(), s_style);

        list.Move(1);

        Assert.Equal(0, list.SelectedIndex);
    }

    [Fact]
    public void TheCursorStopsAtEitherEnd()
    {
        ListView<Label> list = Rows(new RecordingGraphics(), 3);

        list.Move(-1);
        Assert.Equal(0, list.SelectedIndex);

        list.Move(10);
        Assert.Equal(2, list.SelectedIndex);
    }

    [Fact]
    public void AListThatFitsItsWindowDrawsEveryRowAndNeverScrolls()
    {
        RecordingGraphics graphics = new();
        ListView<Label> list = Rows(graphics, 5);
        list.VisibleCount = 5;

        list.Move(4);
        list.Draw();

        Assert.Equal(0, list.FirstVisible);
        Assert.Equal(5, graphics.LeftTexts.Count);
        Assert.False(list.HasRowsAbove);
        Assert.False(list.HasRowsBelow);
    }

    [Fact]
    public void NoWindowDrawsEveryRow()
    {
        RecordingGraphics graphics = new();
        ListView<Label> list = Rows(graphics, 5);

        list.Draw();

        Assert.Equal(5, graphics.LeftTexts.Count);
    }

    [Fact]
    public void AWindowDrawsOnlyWhatFits()
    {
        RecordingGraphics graphics = new();
        ListView<Label> list = Rows(graphics, 10);
        list.VisibleCount = 3;

        list.Draw();

        Assert.Equal(3, graphics.LeftTexts.Count);
        Assert.False(list.HasRowsAbove);
        Assert.True(list.HasRowsBelow);
    }

    [Fact]
    public void TheWindowFollowsTheCursorDownByTheLeastItCan()
    {
        RecordingGraphics graphics = new();
        ListView<Label> list = Rows(graphics, 10);
        list.VisibleCount = 3;

        // Rows 0, 1 and 2 are shown, so the cursor reaching row 3 moves the
        // window by exactly one.
        list.Move(3);
        list.Draw();

        Assert.Equal(1, list.FirstVisible);
        Assert.True(list.HasRowsAbove);
    }

    [Fact]
    public void TheWindowFollowsTheCursorBackUp()
    {
        RecordingGraphics graphics = new();
        ListView<Label> list = Rows(graphics, 10);
        list.VisibleCount = 3;
        list.Move(9);
        list.Draw();

        Assert.Equal(7, list.FirstVisible);

        list.SelectedIndex = 0;
        list.Draw();

        Assert.Equal(0, list.FirstVisible);
    }

    [Fact]
    public void OnlyTheSelectedRowIsDrawnSelected()
    {
        RecordingGraphics graphics = new();
        ListView<Label> list = Rows(graphics, 4);
        list.Move(2);

        list.Draw();

        Assert.Equal(s_highlight, Assert.Single(graphics.FilledRectangles).Colour);
    }

    [Fact]
    public void ClearingPutsTheCursorBack()
    {
        ListView<Label> list = Rows(new RecordingGraphics(), 5);
        list.Move(4);

        list.Clear();

        Assert.Equal(0, list.SelectedIndex);
        Assert.Equal(0, list.FirstVisible);
        Assert.Empty(list.Rows);
    }

    private static ListView<Label> Rows(RecordingGraphics graphics, int count)
    {
        ListView<Label> list = new(graphics, s_style) { Width = 100, Height = 8, Spacing = 8 };

        for (int i = 0; i < count; i++)
        {
            list.Add(new Label(graphics, s_style, new TextSetting($"Row {i}")));
        }

        return list;
    }
}
