// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Graphics;
using SharpKind.Input;

namespace SharpKind.UI.Gallery;

/// <summary>
/// Every control, on one screen, with each one's bounds outlined. The outline
/// is the point: alignment and state are claims about where a thing sits
/// inside its own box, and without the box drawn there is nothing to judge
/// them against.
/// <para>
/// The layout is in rows, and a row is however tall the font actually is -
/// measured, not assumed. The two backends do not share a font: the software
/// one draws from an 8x8 bitmap sheet, the hardware one from a 12pt true-type
/// face that is around twice as tall, and a pitch hardcoded to either would
/// pile the rows on top of each other in the other. The same measurement
/// decides whether text snaps to a character cell, since only the fixed-cell
/// font has a grid to snap to.
/// </para>
/// <para>
/// Nothing here is told what to show. Every control is bound to a setting and
/// reads through it as it draws, which is why the clock ticks and the pressed
/// count rises without the gallery touching a control after building it.
/// </para>
/// </summary>
internal sealed class Gallery
{
    /// <summary>
    /// How many rows the gallery lays out in, which is what the canvas has to
    /// be tall enough for. Named here rather than where the window is sized
    /// because it is a fact about this layout, not about the window.
    /// </summary>
    internal const int LayoutRows = 41;

    private const string Font = "Small";

    // The horizontal grid stays in 8s whatever the font: they are the
    // gallery's own margins, not the font's.
    private const int Cell = 8;
    private const int BoxLeft = 9 * Cell;
    private const int BoxWidth = 22 * Cell;
    private const int ComboLeft = 4 * Cell;
    private const int ComboWidth = 32 * Cell;
    private const int ScreenColumns = 40;

    // Tall and deep, so the measurement is of the line rather than of one
    // short glyph.
    private const string MeasuringText = "Ag";

    private static readonly FastColor s_white = new(0xFFFFFFFF);
    private static readonly FastColor s_grey = new(0xFF909090);
    private static readonly FastColor s_red = new(0xFFAA0000);
    private static readonly FastColor s_yellow = new(0xFFFFFF00);
    private static readonly FastColor s_outline = new(0xFF303030);

    private readonly IGraphics _graphics;
    private readonly ControlStyle _style;
    private readonly float _pitch;
    private readonly float _snap;
    private readonly Container<Label> _alignment;
    private readonly Container<Label> _states;
    private readonly Container<ComboBox> _combos;
    private readonly Container<Button> _buttons;
    private readonly Container<TextBox> _textBoxes;
    private readonly Container<Label> _centred;
    private readonly Container<Label> _clock;

    // The rows the cursor visits, in the order it visits them. Flat and mixed,
    // because the cursor does not care which section a row was drawn in.
    private readonly List<UIControl> _rows = [];

    private int _focus;

    internal Gallery(IGraphics graphics, ScreenLayout screen)
    {
        _graphics = graphics;

        // Held to what the canvas can actually show, since the window was sized from an estimate of the font's height.
        float lineHeight = MathF.Max(Cell, graphics.MeasureText(MeasuringText, Font).Y);
        _pitch = MathF.Min(lineHeight, screen.ScreenHeight / LayoutRows);

        // A proportional font has no grid to sit on; a line taller than the cell is how this tells the two apart.
        _snap = _pitch > Cell ? 0 : Cell;

        // Every state is given a look, so that every state is visible: the
        // selected ones carry a block, the disabled ones grey text.
        _style = new(
            Font,
            ControlColors.TextOnly(s_white),
            new(s_red, s_white),
            ControlColors.TextOnly(s_grey),
            new(s_red, s_grey));

        _alignment = new(graphics, _style) { ChildAlignment = TextAlignment.Left };
        _states = new(graphics, _style) { ChildAlignment = TextAlignment.Left };
        _combos = new(graphics, _style) { ChildAlignment = TextAlignment.Left };
        _buttons = new(graphics, _style) { ChildAlignment = TextAlignment.Centre };
        _textBoxes = new(graphics, _style) { ChildAlignment = TextAlignment.Centre };
        _centred = new(graphics, _style) { ChildAlignment = TextAlignment.Centre };
        _clock = new(graphics, _style) { ChildAlignment = TextAlignment.Centre };

        // Demonstration rows are spaced a blank row apart so each reads as its own box; combo rows are not, since a settings list is a list.
        _alignment.Spacing = 2 * _pitch;
        _states.Spacing = 2 * _pitch;
        _centred.Spacing = 2 * _pitch;
        _combos.Spacing = _pitch;
        _buttons.Spacing = 2 * _pitch;
        _textBoxes.Spacing = 2 * _pitch;

        BuildAlignment();
        BuildStates();
        BuildCombos();
        BuildButtons();
        BuildTextBoxes();
        BuildCentred();
        BuildClock();

        _rows.AddRange(_combos.Children);
        _rows.AddRange(_buttons.Children);
        _rows.AddRange(_textBoxes.Children);
    }

    /// <summary>
    /// Up and Down move between the rows that take input; what the row does
    /// with the rest of the keys is the control's own business, not the
    /// gallery's.
    /// <para>
    /// S and X do what Up and Down do, as they do throughout Elite: the arrow
    /// keys are extended-key codes, which the screenshot harness cannot post
    /// to an SDL window, so a gallery reachable only by arrows could not be
    /// driven by the thing that takes pictures of it. The focused row is
    /// offered the keys first, so on the text box those two letters are typed
    /// rather than navigated with - Tab always moves on.
    /// </para>
    /// </summary>
    /// <param name="keyboard">The keys pressed this tick.</param>
    internal void HandleInput(IKeyboard keyboard)
    {
        switch (_rows[_focus])
        {
            case ComboBox combo:
                combo.HandleInput(keyboard);
                break;

            case Button button:
                button.HandleInput(keyboard);
                break;

            case TextBox textBox:
                textBox.HandleInput(keyboard);
                break;
        }

        if ((keyboard.IsPressed(ConsoleKey.UpArrow) || keyboard.IsPressed(ConsoleKey.S)) && _focus > 0)
        {
            _focus--;
        }

        bool down = keyboard.IsPressed(ConsoleKey.DownArrow)
            || keyboard.IsPressed(ConsoleKey.X)
            || keyboard.IsPressed(ConsoleKey.Tab);

        if (down && _focus < _rows.Count - 1)
        {
            _focus++;
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            _rows[i].State = i == _focus ? ControlState.Selected : ControlState.Normal;
        }
    }

    /// <summary>
    /// Draws the whole gallery, top to bottom. The vertical position is a
    /// running total of rows rather than a set of fixed numbers, so a taller
    /// font moves everything below it down instead of overlapping it.
    /// </summary>
    internal void Draw()
    {
        float y = 0;

        y = DrawHeading(y, "CONTROL GALLERY", s_yellow);

        y = DrawHeading(y, "LABEL ALIGNMENT", s_yellow);
        y = DrawSection(y, _alignment, BoxLeft, BoxWidth);

        y = DrawHeading(y, "LABEL STATES", s_yellow);
        y = DrawSection(y, _states, BoxLeft, BoxWidth);

        y = DrawHeading(y, "COMBOBOX", s_yellow);
        y = DrawSection(y, _combos, ComboLeft, ComboWidth);

        y = DrawHeading(y, "BUTTON", s_yellow);
        y = DrawSection(y, _buttons, BoxLeft, BoxWidth);

        y = DrawHeading(y, "TEXTBOX", s_yellow);
        y = DrawSection(y, _textBoxes, BoxLeft, BoxWidth);

        y = DrawHeading(y, "CONTAINER - CHILDREN CENTRED", s_yellow);
        y = DrawSection(y, _centred, BoxLeft, BoxWidth);

        y = DrawHeading(y, "LABEL - BOUND TO A CLOCK", s_yellow);
        y = DrawSection(y, _clock, BoxLeft, BoxWidth);

        y = DrawHeading(y, "Up/Down, S/X or Tab select a row", s_white);
        y = DrawHeading(y, "Left/Right or , / . cycle it", s_white);
        DrawHeading(y, "Esc to quit", s_white);
    }

    // Section headings are chrome, not controls: each wants its own colour, so they draw straight onto the surface.
    private float DrawHeading(float y, string text, in FastColor colour)
    {
        float width = _graphics.MeasureText(text, Font).X;
        float left = (ScreenColumns * Cell / 2f) - (width / 2);

        if (_snap > 0)
        {
            left = MathF.Floor(left / _snap) * _snap;
        }

        _graphics.DrawTextLeft(new(left, y), text, Font, colour);

        return y + _pitch;
    }

    private float DrawSection<TControl>(float y, Container<TControl> container, float left, float width)
        where TControl : UIControl
    {
        container.Position = new(left, y);
        container.Width = width;
        container.Draw();
        Outline(container);

        // Every child but the last is followed by its spacing, and the last
        // occupies a row of its own.
        return y + ((container.Children.Count - 1) * container.Spacing) + (2 * _pitch);
    }

    // Every control's bounds, so that what a label did with its width can be
    // seen rather than taken on trust.
    private void Outline<TControl>(Container<TControl> container)
        where TControl : UIControl
    {
        foreach (TControl child in container.Children)
        {
            _graphics.DrawRectangle(child.Position, child.Width, child.Height, s_outline);
        }
    }

    private void BuildAlignment()
    {
        foreach (TextAlignment alignment in Enum.GetValues<TextAlignment>())
        {
            _alignment.Add(new Label(_graphics, _style, new TextSetting(alignment.ToString()))
            {
                Alignment = alignment,
                Width = BoxWidth,
                Height = _pitch,
                SnapToCell = _snap,
            });
        }
    }

    private void BuildStates()
    {
        foreach (ControlState state in Enum.GetValues<ControlState>())
        {
            _states.Add(new Label(_graphics, _style, new TextSetting(state.ToString()))
            {
                Alignment = TextAlignment.Centre,
                State = state,
                Width = BoxWidth,
                Height = _pitch,
                SnapToCell = _snap,
            });
        }
    }

    private void BuildCombos()
    {
        // The last has one value, which is how a row with nothing to cycle to
        // shows: selected, but with no arrows offered.
        MemorySetting[] settings =
        [
            new("Graphic Style:", "Wireframe", "Solid"),
            new("Depth Sort:", "Painter", "ZBuffer"),
            new("Rendition *:", "8-bit"),
        ];

        foreach (MemorySetting setting in settings)
        {
            _combos.Add(new ComboBox(_graphics, _style, setting)
            {
                Width = ComboWidth,
                Height = _pitch,
                ValueOffsetX = 17 * Cell,
                ArrowGap = Cell,
                SnapToCell = _snap,
            });
        }
    }

    // Bound to a setting that counts how often it has been applied, so that a
    // press shows: the button itself does nothing but apply the binding.
    private void BuildButtons() => _buttons.Add(new Button(_graphics, _style, new PressSetting())
    {
        Width = BoxWidth,
        Height = _pitch,
        SnapToCell = _snap,
    });

    private void BuildTextBoxes() => _textBoxes.Add(new TextBox(_graphics, _style, new TextSetting("JAMESON"))
    {
        Alignment = TextAlignment.Centre,
        Width = BoxWidth,
        Height = _pitch,
        MaxLength = 12,
        SnapToCell = _snap,
    });

    private void BuildCentred()
    {
        foreach (string text in (string[])["Narrow", "A bit wider", "Wider again still"])
        {
            _centred.Add(new Label(_graphics, _style, new TextSetting(text))
            {
                Alignment = TextAlignment.Left,
                Width = _graphics.MeasureText(text, Font).X,
                Height = _pitch,
            });
        }
    }

    // Sized to the widest the time ever gets rather than to whatever it reads
    // now, so a centred clock does not shuffle sideways as the digits change.
    private void BuildClock() => _clock.Add(new Label(_graphics, _style, new ClockSetting())
    {
        Alignment = TextAlignment.Centre,
        Width = BoxWidth,
        Height = _pitch,
        SnapToCell = _snap,
    });
}
