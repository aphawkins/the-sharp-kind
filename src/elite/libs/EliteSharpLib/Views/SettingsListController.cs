// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using SharpKind.Input;
using SharpKind.UI;

namespace EliteSharpLib.Views;

/// <summary>
/// A single-column list of settings with a Back row under it: the cursor and
/// navigation the engine and game settings screens share. Each screen
/// supplies its own settings, already bound to whatever stores them, and this
/// owns the controls that show and cycle them.
/// <para>
/// The controls live here rather than in a rendition because a setting's value
/// is not a rendition's to hold: both tiers can be installed, and one answer
/// to "what is Graphic Style set to" has to serve both. What the rendition
/// supplies is a <see cref="SettingsListStyle"/> - every colour and position,
/// and nothing else - so the screen is still authored per tier without the
/// state being duplicated per tier.
/// </para>
/// <para>
/// Cycling is the control's: this moves the cursor and hands the keys to the
/// row it is on. Left and Right now step opposite ways, which they did not
/// before - the enums behind these settings only offered a Next, so both keys
/// used to advance; a bound setting is an index, which can go either way.
/// </para>
/// </summary>
internal abstract class SettingsListController : IScreenController
{
    private readonly GameState _gameState;
    private readonly IKeyboard _keyboard;
    private readonly IBaseView _baseView;
    private readonly string _header;
    private readonly string _footer;
    private readonly Container<ComboBox> _rows;
    private readonly Label _back;
    private readonly Label _footerLabel;

    protected SettingsListController(
        GameState gameState,
        IKeyboard keyboard,
        IBaseView baseView,
        IViewSurface surface,
        SettingsListStyle style,
        string header,
        IReadOnlyList<ISetting> settings,
        string footer = "")
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(settings);

        _gameState = gameState;
        _keyboard = keyboard;
        _baseView = baseView;
        _header = header;
        _footer = footer;

        _rows = new(surface.Graphics, style.RowStyle)
        {
            ChildAlignment = TextAlignment.Left,
            Position = new(style.RowsLeft, style.FirstRowY),
            Width = style.RowWidth,
            Spacing = style.RowHeight,
        };

        foreach (ISetting setting in settings)
        {
            _rows.Add(new ComboBox(surface.Graphics, style.RowStyle, setting)
            {
                Width = style.RowWidth,
                Height = style.RowHeight,
                ValueOffsetX = style.ValueOffsetX,
                ArrowGap = style.ArrowGap,
                SnapToCell = style.SnapToCell,
            });
        }

        // The Back row is the same on every screen, so it is here rather than
        // repeated in each. A label rather than a combo box: its text is
        // fixed, so its binding is only somewhere for that text to live.
        _back = new(surface.Graphics, style.RowStyle, new TextSetting("Back"))
        {
            Alignment = TextAlignment.Centre,
            Position = new(surface.Layout.ViewportCentre.X - (style.BackRowWidth / 2), style.BackRowY),
            Width = style.BackRowWidth,
            Height = style.RowHeight,
            SnapToCell = style.SnapToCell,
        };

        _footerLabel = new(surface.Graphics, style.ValueStyle, new TextSetting(footer))
        {
            Alignment = TextAlignment.Centre,
            Position = new(surface.Layout.ViewportLeft, style.FooterY),
            Width = surface.Layout.ViewportWidth,
            SnapToCell = style.SnapToCell,
        };
    }

    // Exposed for tests: which row the cursor is on, and the settings behind
    // the rows.
    internal int HighlightedItem { get; private set; }

    internal IReadOnlyList<ISetting> Settings => [.. _rows.Children.Select(row => row.Setting)];

    private int BackIndex => _rows.Children.Count;

    // The settings, plus the Back row under them.
    private int RowCount => _rows.Children.Count + 1;

    public void Draw()
    {
        _baseView.DrawViewHeader(_header);

        for (int i = 0; i < _rows.Children.Count; i++)
        {
            _rows.Children[i].State = i == HighlightedItem ? ControlState.Selected : ControlState.Normal;
        }

        _rows.Draw();

        _back.State = HighlightedItem == BackIndex ? ControlState.Selected : ControlState.Normal;
        _back.Draw();

        if (_footer.Length > 0)
        {
            _footerLabel.Draw();
        }
    }

    public void HandleInput()
    {
        if (_keyboard.IsPressed(ConsoleKey.S) || _keyboard.IsPressed(ConsoleKey.UpArrow))
        {
            SelectUp();
        }

        if (_keyboard.IsPressed(ConsoleKey.X) || _keyboard.IsPressed(ConsoleKey.DownArrow))
        {
            SelectDown();
        }

        if (HighlightedItem == BackIndex)
        {
            if (_keyboard.IsPressed(ConsoleKey.Enter))
            {
                _gameState.SetView(Screen.Options);
            }

            return;
        }

        _rows.Children[HighlightedItem].HandleInput(_keyboard);
    }

    public void Reset() => HighlightedItem = 0;

    public void Update()
    {
    }

    // The cursor wraps rather than stopping at the ends, as the options menu's
    // does: Back is the last row on every one of these screens, so one press
    // up from the first setting is the shortest way out.
    private void SelectDown() => HighlightedItem = (HighlightedItem + 1) % RowCount;

    private void SelectUp() => HighlightedItem = (HighlightedItem + RowCount - 1) % RowCount;
}
