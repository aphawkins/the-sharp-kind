// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Graphics;
using SharpKind.Input;

namespace SharpKind.UI;

/// <summary>
/// A named setting and the value it is currently on, with arrows either side
/// of the value while the cursor is on the row to show that there are others
/// to cycle to. There is no drop-down: this is a keyboard menu, so the
/// alternatives are stepped through in place rather than listed.
/// <para>
/// The box holds no value of its own. Everything it shows is read from the
/// <see cref="ISetting"/> it is bound to at the moment it draws, and cycling
/// writes straight back through it - so the control owns the interaction while
/// the binding owns the truth. What it does hold is how it draws: its bounds,
/// its colours and the labels it draws with.
/// </para>
/// </summary>
public sealed class ComboBox : UIControl
{
    private readonly ControlStyle _valueStyle;
    private readonly Label _name;
    private readonly Label _value;
    private readonly Label _openArrow;
    private readonly Label _closeArrow;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComboBox"/> class.
    /// </summary>
    /// <param name="graphics">The surface to draw on.</param>
    /// <param name="style">The row's font and colours.</param>
    /// <param name="setting">The setting this row shows and cycles.</param>
    public ComboBox(IGraphics graphics, ControlStyle style, ISetting setting)
        : base(graphics, style, setting)
    {
        // The name label fills the row, so anything drawn after must not paint another background - strips it itself, not via a caller style.
        _valueStyle = style.WithoutBackground();

        _name = new(graphics, style, setting);
        _value = new(graphics, _valueStyle, new ValueOf(setting));

        // Zero-width boxes: right/left-aligned arrows place against the value itself, not fixed columns that would strand them when short.
        _openArrow = new(graphics, _valueStyle, new TextSetting("<")) { Alignment = TextAlignment.Right };
        _closeArrow = new(graphics, _valueStyle, new TextSetting(">")) { Alignment = TextAlignment.Left };
    }

    /// <summary>
    /// Gets or sets the value's distance from the row's left edge.
    /// </summary>
    public float ValueOffsetX { get; set; }

    /// <summary>
    /// Gets or sets the space between an arrow and the value.
    /// </summary>
    public float ArrowGap { get; set; }

    /// <summary>
    /// Gets or sets the character cell the value's arrows are rounded to, or
    /// zero for none.
    /// </summary>
    public float SnapToCell { get; set; }

    /// <summary>
    /// Gets the value currently selected, or empty if there are none.
    /// </summary>
    public string Value => Setting.Value;

    /// <summary>
    /// Applies the keys this row answers to while the cursor is on it: Left
    /// and Right - or comma and full stop, which the keyboard delivers where
    /// the arrow keys cannot reach - step between values, and Enter steps
    /// forwards.
    /// </summary>
    /// <param name="keyboard">The keys pressed this tick.</param>
    public void HandleInput(IKeyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        if (keyboard.IsPressed(ConsoleKey.RightArrow)
            || keyboard.IsPressed(ConsoleKey.OemPeriod)
            || keyboard.IsPressed(ConsoleKey.Enter))
        {
            Next();
        }

        if (keyboard.IsPressed(ConsoleKey.LeftArrow) || keyboard.IsPressed(ConsoleKey.OemComma))
        {
            Previous();
        }
    }

    /// <summary>
    /// Moves to the next value, wrapping back to the first past the end.
    /// </summary>
    public void Next()
    {
        if (Setting.Values.Count > 0)
        {
            Setting.SelectedIndex = (Setting.SelectedIndex + 1) % Setting.Values.Count;
        }
    }

    /// <summary>
    /// Moves to the previous value, wrapping to the last before the first.
    /// </summary>
    public void Previous()
    {
        if (Setting.Values.Count > 0)
        {
            Setting.SelectedIndex = (Setting.SelectedIndex - 1 + Setting.Values.Count) % Setting.Values.Count;
        }
    }

    /// <summary>
    /// Draws the row at its current position, in its current state.
    /// </summary>
    public override void Draw()
    {
        _name.Position = Position;
        _name.Width = Width;
        _name.Height = Height;
        _name.State = State;
        _name.Draw();

        _value.Position = new(Position.X + ValueOffsetX, Position.Y);
        _value.Width = Width - ValueOffsetX;
        _value.Height = Height;
        _value.State = State;
        _value.Draw();

        if (State is ControlState.Selected or ControlState.SelectedDisabled)
        {
            DrawArrows();
        }
    }

    // Only worth showing when there is somewhere else to go: a setting with a
    // single value is not something the player can cycle.
    private void DrawArrows()
    {
        if (Setting.Values.Count < 2)
        {
            return;
        }

        float valueLeft = Position.X + ValueOffsetX;
        float valueWidth = Graphics.MeasureText(Value, _valueStyle.FontType).X;

        _openArrow.Position = new(valueLeft - ArrowGap, Position.Y);
        _openArrow.Height = Height;
        _openArrow.State = State;
        _openArrow.SnapToCell = SnapToCell;
        _openArrow.Draw();

        _closeArrow.Position = new(valueLeft + valueWidth + ArrowGap, Position.Y);
        _closeArrow.Height = Height;
        _closeArrow.State = State;
        _closeArrow.SnapToCell = SnapToCell;
        _closeArrow.Draw();
    }

    // The setting seen from the value end - read through, never copied, so the label can't show a value the setting has moved off.
    private sealed class ValueOf(ISetting setting) : ISetting
    {
        public string Name => setting.Value;

        public IReadOnlyList<string> Values => setting.Values;

        public int SelectedIndex
        {
            get => setting.SelectedIndex;
            set => setting.SelectedIndex = value;
        }
    }
}
