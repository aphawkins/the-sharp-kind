// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Fakes.Input;
using SharpKind.Graphics.Fakes;

namespace SharpKind.UI.Tests;

public class ControlTests
{
    private static readonly FastColor s_text = new(0xFFFFFFFF);
    private static readonly FastColor s_disabled = new(0xFF808080);
    private static readonly FastColor s_highlight = new(0xFFFF0000);

    private static readonly ControlStyle s_style = new(
        "Small",
        ControlColors.TextOnly(s_text),
        new(s_highlight, s_text),
        ControlColors.TextOnly(s_disabled));

    [Fact]
    public void ANormalLabelDrawsNoBackground()
    {
        RecordingGraphics graphics = new();
        Label label = Bounded(graphics);

        label.Draw();

        Assert.Empty(graphics.FilledRectangles);
        Assert.Equal(s_text, Assert.Single(graphics.LeftTexts).Colour);
    }

    [Fact]
    public void ASelectedLabelFillsItsOwnBounds()
    {
        RecordingGraphics graphics = new();
        Label label = Bounded(graphics);
        label.State = ControlState.Selected;

        label.Draw();

        (Vector2 position, float width, float height, FastColor colour) = Assert.Single(graphics.FilledRectangles);
        Assert.Equal(new Vector2(10, 20), position);
        Assert.Equal(100, width);
        Assert.Equal(8, height);
        Assert.Equal(s_highlight, colour);
    }

    [Fact]
    public void ADisabledLabelKeepsTheCursorBlockWhenItIsAlsoSelected()
    {
        RecordingGraphics graphics = new();
        Label label = Bounded(graphics);
        label.State = ControlState.SelectedDisabled;

        label.Draw();

        Assert.Equal(s_highlight, Assert.Single(graphics.FilledRectangles).Colour);
        Assert.Equal(s_disabled, Assert.Single(graphics.LeftTexts).Colour);
    }

    [Theory]
    [InlineData(TextAlignment.Left, 10)]
    [InlineData(TextAlignment.Centre, 44)]
    [InlineData(TextAlignment.Right, 78)]
    public void TextIsAlignedWithinTheLabelsOwnBounds(TextAlignment alignment, float expectedX)
    {
        RecordingGraphics graphics = new();
        Label label = Bounded(graphics);
        label.Alignment = alignment;

        label.Draw();

        // "Abcd" measures 32 of the label's 100, at x=10.
        Assert.Equal(new Vector2(expectedX, 20), Assert.Single(graphics.LeftTexts).Position);
    }

    [Fact]
    public void CentringRoundsToTheCellWhenTheLabelSnaps()
    {
        RecordingGraphics graphics = new();
        Label label = Bounded(graphics, "Ab");
        label.Alignment = TextAlignment.Centre;
        label.SnapToCell = 8;

        label.Draw();

        // Centring puts the text at 52, not on a cell boundary, so it truncates to 48. Snapping is of the final position, not the gap.
        Assert.Equal(new Vector2(48, 20), Assert.Single(graphics.LeftTexts).Position);
    }

    [Fact]
    public void ContainerStacksItsChildrenAtItsSpacing()
    {
        RecordingGraphics graphics = new();
        Container<Label> container = new(graphics, s_style) { Position = new(10, 20), Width = 100, Spacing = 16 };
        container.Add(Bounded(graphics));
        container.Add(Bounded(graphics));

        container.Layout();

        Assert.Equal(new Vector2(10, 20), container.Children[0].Position);
        Assert.Equal(new Vector2(10, 36), container.Children[1].Position);
    }

    [Fact]
    public void ContainerAlignsEachChildAcrossItsWidth()
    {
        RecordingGraphics graphics = new();
        Container<Label> container = new(graphics, s_style)
        {
            Position = new(10, 20),
            Width = 100,
            ChildAlignment = TextAlignment.Centre,
        };

        Label child = Bounded(graphics);
        child.Width = 40;
        container.Add(child);

        container.Layout();

        Assert.Equal(new Vector2(40, 20), child.Position);
    }

    [Fact]
    public void ContainerDrawsEveryChild()
    {
        RecordingGraphics graphics = new();
        Container<Label> container = new(graphics, s_style) { Width = 100, Spacing = 8 };
        container.Add(Bounded(graphics));
        container.Add(Bounded(graphics));

        container.Draw();

        Assert.Equal(2, graphics.LeftTexts.Count);
    }

    [Fact]
    public void AComboBoxDrawsItsValueAtTheGivenOffsetAndNoArrowsWhenUnselected()
    {
        RecordingGraphics graphics = new();
        ComboBox box = Combo(graphics);

        box.Draw();

        Assert.Collection(
            graphics.LeftTexts,
            name => Assert.Equal(new Vector2(10, 20), name.Position),
            value =>
            {
                Assert.Equal("Solid", value.Text);
                Assert.Equal(new Vector2(70, 20), value.Position);
            });
    }

    [Fact]
    public void AComboBoxBracketsItsValueWhenSelected()
    {
        RecordingGraphics graphics = new();
        ComboBox box = Combo(graphics);
        box.State = ControlState.Selected;

        box.Draw();

        // Arrows hug the value rather than sitting at fixed columns: value starts at 70, "Solid" measures 40.
        Assert.Equal(["Graphic Style:", "Solid", "<", ">"], graphics.LeftTexts.Select(t => t.Text));
        Assert.Equal(new Vector2(52, 20), graphics.LeftTexts[2].Position);
        Assert.Equal(new Vector2(120, 20), graphics.LeftTexts[3].Position);
    }

    [Fact]
    public void AComboBoxWithOneValueOffersNothingToCycleTo()
    {
        RecordingGraphics graphics = new();
        ComboBox box = Combo(graphics, new FakeSetting("Graphic Style:", "Solid"));
        box.State = ControlState.Selected;

        box.Draw();

        Assert.Equal(["Graphic Style:", "Solid"], graphics.LeftTexts.Select(t => t.Text));
    }

    [Fact]
    public void AComboBoxFillsItsWholeWidthWhenSelected()
    {
        RecordingGraphics graphics = new();
        ComboBox box = Combo(graphics);
        box.State = ControlState.Selected;

        box.Draw();

        // The name's label carries the block for the whole row; everything
        // else draws over it, so nothing repaints a second one.
        (Vector2 position, float width, _, _) = Assert.Single(graphics.FilledRectangles);
        Assert.Equal(new Vector2(10, 20), position);
        Assert.Equal(100, width);
    }

    [Fact]
    public void AComboBoxCyclesThroughItsValuesBothWays()
    {
        RecordingGraphics graphics = new();
        ComboBox box = Combo(graphics);

        Assert.Equal("Solid", box.Value);

        box.Next();
        Assert.Equal("Wireframe", box.Value);

        box.Next();
        Assert.Equal("Solid", box.Value);

        box.Previous();
        Assert.Equal("Wireframe", box.Value);
    }

    [Fact]
    public void AComboBoxWithNoValuesShowsNothing()
    {
        RecordingGraphics graphics = new();
        ComboBox box = Combo(graphics, new FakeSetting("Nothing"));

        box.Next();
        box.Previous();

        Assert.Equal(string.Empty, box.Value);
    }

    [Fact]
    public void AComboBoxWritesStraightBackToItsBinding()
    {
        RecordingGraphics graphics = new();
        FakeSetting setting = new("Graphic Style:", "Solid", "Wireframe");
        ComboBox box = Combo(graphics, setting);

        box.Next();

        // The control keeps no copy: the binding is the only place the choice
        // is recorded, so this is what the application sees.
        Assert.Equal(1, setting.SelectedIndex);
        Assert.Equal("Wireframe", box.Value);
    }

    [Fact]
    public void AComboBoxShowsWhateverItsBindingSaysNow()
    {
        RecordingGraphics graphics = new();
        FakeSetting setting = new("Graphic Style:", "Solid", "Wireframe");
        ComboBox box = Combo(graphics, setting);

        // Changed behind the control's back, as the config file being reloaded
        // would: the next draw has to follow it.
        setting.SelectedIndex = 1;

        Assert.Equal("Wireframe", box.Value);
    }

    [Fact]
    public void ControlsRejectAMissingSurfaceStyleOrBinding()
    {
        RecordingGraphics graphics = new();
        FakeSetting setting = new("Abcd");

        Assert.Throws<ArgumentNullException>(() => new Label(null!, s_style, setting));
        Assert.Throws<ArgumentNullException>(() => new Label(graphics, null!, setting));
        Assert.Throws<ArgumentNullException>(() => new Label(graphics, s_style, null!));
    }

    [Fact]
    public void ALabelShowsWhateverItsBindingSaysNow()
    {
        RecordingGraphics graphics = new();
        TextSetting setting = new("Before");
        Label label = new(graphics, s_style, setting) { Width = 100, Height = 8 };

        setting.Value = "After";
        label.Draw();

        // Nothing was assigned to the label: it asks the binding as it draws,
        // which is what lets a clock tick without being told to.
        Assert.Equal("After", Assert.Single(graphics.LeftTexts).Text);
    }

    [Fact]
    public void AButtonAppliesItsBindingWhenPressed()
    {
        RecordingGraphics graphics = new();
        FakeSetting setting = new("Save", "Saved") { SelectedIndex = -1 };
        Button button = new(graphics, s_style, setting);

        button.Press();

        Assert.Equal(0, setting.SelectedIndex);
    }

    [Fact]
    public void AButtonPressesOnEnterButNotWhileDisabled()
    {
        RecordingGraphics graphics = new();
        FakeSetting setting = new("Save", "Saved") { SelectedIndex = -1 };
        Button button = new(graphics, s_style, setting) { State = ControlState.SelectedDisabled };
        FakeKeyboard keyboard = new();

        keyboard.KeyDown(ConsoleKey.Enter, ConsoleModifiers.None);
        button.HandleInput(keyboard);
        Assert.Equal(-1, setting.SelectedIndex);

        button.State = ControlState.Selected;
        keyboard.KeyDown(ConsoleKey.Enter, ConsoleModifiers.None);
        button.HandleInput(keyboard);
        Assert.Equal(0, setting.SelectedIndex);
    }

    [Fact]
    public void ATextBoxTypesIntoItsBinding()
    {
        RecordingGraphics graphics = new();
        TextSetting setting = new();
        TextBox box = new(graphics, s_style, setting);
        FakeKeyboard keyboard = new();

        foreach (ConsoleKey key in (ConsoleKey[])[ConsoleKey.A, ConsoleKey.Spacebar, ConsoleKey.D1])
        {
            keyboard.KeyDown(key, ConsoleModifiers.None);
            box.HandleInput(keyboard);
            keyboard.KeyUp(key, ConsoleModifiers.None);
        }

        Assert.Equal("A 1", setting.Value);
    }

    [Fact]
    public void ATextBoxBackspacesAndStopsAtItsLimit()
    {
        RecordingGraphics graphics = new();
        TextSetting setting = new("ABC");
        TextBox box = new(graphics, s_style, setting) { MaxLength = 3 };
        FakeKeyboard keyboard = new();

        // Full, so the D has nowhere to go.
        keyboard.KeyDown(ConsoleKey.D, ConsoleModifiers.None);
        box.HandleInput(keyboard);
        Assert.Equal("ABC", setting.Value);

        keyboard.ClearPressed();
        keyboard.KeyDown(ConsoleKey.Backspace, ConsoleModifiers.None);
        box.HandleInput(keyboard);
        Assert.Equal("AB", setting.Value);
    }

    [Fact]
    public void ATextBoxShowsACursorOnlyWhileSelected()
    {
        RecordingGraphics graphics = new();
        TextBox box = new(graphics, s_style, new TextSetting("AB")) { Width = 100, Height = 8 };

        box.Draw();
        Assert.Equal("AB", graphics.LeftTexts[0].Text);

        box.State = ControlState.Selected;
        box.Draw();
        Assert.Equal("AB_", graphics.LeftTexts[1].Text);
    }

    [Fact]
    public void ASettingsValueIsItsChosenValueUnlessItStoresOneOfItsOwn()
    {
        // Through the interface, since the derived value is a default member
        // of it rather than of whatever implements it.
        ISetting choice = new FakeSetting("Graphic Style:", "Solid", "Wireframe");

        Assert.Equal("Solid", choice.Value);

        choice.Value = "Wireframe";
        Assert.Equal(1, choice.SelectedIndex);

        // A value it does not have is not a choice, so nothing moves.
        choice.Value = "Vector";
        Assert.Equal(1, choice.SelectedIndex);
    }

    [Fact]
    public void AStyleWithoutBackgroundKeepsEveryTextColour()
    {
        ControlStyle stripped = s_style.WithoutBackground();

        foreach (ControlState state in Enum.GetValues<ControlState>())
        {
            Assert.False(stripped.Colors(state).HasBackground);
            Assert.Equal(s_style.Colors(state).Text, stripped.Colors(state).Text);
        }
    }

    private static ComboBox Combo(RecordingGraphics graphics, ISetting? setting = null)
        => new(graphics, s_style, setting ?? new FakeSetting("Graphic Style:", "Solid", "Wireframe"))
        {
            Position = new(10, 20),
            Width = 100,
            Height = 8,
            ValueOffsetX = 60,
            ArrowGap = 10,
        };

    private static Label Bounded(RecordingGraphics graphics, string text = "Abcd")
        => new(graphics, s_style, new TextSetting(text))
        {
            Position = new(10, 20),
            Width = 100,
            Height = 8,
        };
}
