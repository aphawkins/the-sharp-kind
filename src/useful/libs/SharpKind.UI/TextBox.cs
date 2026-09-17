// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using SharpKind.Graphics;
using SharpKind.Input;

namespace SharpKind.UI;

/// <summary>
/// A line of text the player types into - a commander's name, a save slot.
/// Unlike a <see cref="ComboBox"/> there is no list to step through, so the
/// binding's <see cref="ISetting.Value"/> is written directly and its
/// <see cref="ISetting.Values"/> is empty.
/// <para>
/// The keyboard reports keys rather than characters, so what can be typed is
/// what a key names: letters, digits and space. That is the same alphabet the
/// games' own name entry has always accepted, and it is why there is no
/// shifted punctuation here to be missing.
/// </para>
/// </summary>
public sealed class TextBox : UIControl
{
    // Listed rather than ranged over: ConsoleKey's space, digits and letters are three separate stretches of the enum.
    private static readonly ConsoleKey[] s_typeable =
    [
        ConsoleKey.Spacebar,
        .. Enumerable.Range((int)ConsoleKey.D0, 10).Select(key => (ConsoleKey)key),
        .. Enumerable.Range((int)ConsoleKey.A, 26).Select(key => (ConsoleKey)key),
    ];

    private readonly Label _text;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBox"/> class.
    /// </summary>
    /// <param name="graphics">The surface to draw on.</param>
    /// <param name="style">The box's font and colours.</param>
    /// <param name="setting">The binding this box reads and writes.</param>
    public TextBox(IGraphics graphics, ControlStyle style, ISetting setting)
        : base(graphics, style, setting)
        => _text = new(graphics, style, new Shown(setting, this));

    /// <summary>
    /// Gets or sets the most characters the text may hold. Typing stops at
    /// the limit rather than scrolling: a box that shows all of a short name
    /// is what a menu wants, and there is nowhere for the rest to go.
    /// </summary>
    public int MaxLength { get; set; } = 16;

    /// <summary>
    /// Gets or sets where the text sits within the box.
    /// </summary>
    public TextAlignment Alignment
    {
        get => _text.Alignment;
        set => _text.Alignment = value;
    }

    /// <summary>
    /// Gets or sets the character cell the text is rounded to, or zero for
    /// none.
    /// </summary>
    public float SnapToCell
    {
        get => _text.SnapToCell;
        set => _text.SnapToCell = value;
    }

    /// <summary>
    /// Gets or sets the cursor drawn after the text while the box is
    /// selected, or empty for none.
    /// </summary>
    public string Cursor { get; set; } = "_";

    /// <summary>
    /// Applies the keys this box answers to while the cursor is on it: a
    /// letter, digit or space is appended, and Backspace takes the last
    /// character off. A disabled box takes nothing.
    /// </summary>
    /// <param name="keyboard">The keys pressed this tick.</param>
    public void HandleInput(IKeyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        if (State is ControlState.Disabled or ControlState.SelectedDisabled)
        {
            return;
        }

        if (keyboard.IsPressed(ConsoleKey.Backspace) && Setting.Value.Length > 0)
        {
            Setting.Value = Setting.Value[..^1];
        }

        if (Setting.Value.Length < MaxLength && Typed(keyboard) is string character)
        {
            Setting.Value += character;
        }
    }

    /// <summary>
    /// Draws the box at its current position, in its current state.
    /// </summary>
    public override void Draw()
    {
        _text.Position = Position;
        _text.Width = Width;
        _text.Height = Height;
        _text.State = State;
        _text.Draw();
    }

    // The character typed this tick, or null for none.
    private static string? Typed(IKeyboard keyboard)
    {
        foreach (ConsoleKey key in s_typeable)
        {
            if (keyboard.IsPressed(key))
            {
                return CharacterOf(key);
            }
        }

        return null;
    }

    // D0 to D9 are the digits with a letter in front; A to Z are themselves.
    private static string CharacterOf(ConsoleKey key) => key switch
    {
        ConsoleKey.Spacebar => " ",
        >= ConsoleKey.D0 and <= ConsoleKey.D9 => ((char)('0' + (key - ConsoleKey.D0))).ToString(CultureInfo.InvariantCulture),
        _ => key.ToString(),
    };

    // Read through rather than stored, so the label cannot show a name that has since been typed into.
    private sealed class Shown(ISetting setting, TextBox box) : ISetting
    {
        public string Name => setting.Value + (box.State == ControlState.Selected ? box.Cursor : string.Empty);

        public IReadOnlyList<string> Values => setting.Values;

        public int SelectedIndex
        {
            get => setting.SelectedIndex;
            set => setting.SelectedIndex = value;
        }
    }
}
