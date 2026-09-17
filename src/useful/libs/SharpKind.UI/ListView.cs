// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Graphics;

namespace SharpKind.UI;

/// <summary>
/// A stack of rows with a cursor on one of them and a window onto however many
/// fit. Where a <see cref="Container{TControl}"/> lays every child out and
/// leaves selection to the caller, a list view owns both: it marks the selected
/// row and it scrolls, which are the same question asked twice - a cursor that
/// can leave the window is what makes a window worth having.
/// <para>
/// The window follows the cursor rather than the other way round. There is no
/// scroll position to set: <see cref="SelectedIndex"/> is the state, and the
/// first visible row is whatever keeps the cursor on screen having moved as
/// little as possible. A list shorter than its window never scrolls, which is
/// what makes fitting a list exactly - as the classic seventeen goods do - draw
/// the same as it did before there was a window at all.
/// </para>
/// </summary>
/// <typeparam name="TControl">What this list holds.</typeparam>
/// <param name="graphics">The surface its rows draw on.</param>
/// <param name="style">The look its rows are built with.</param>
public sealed class ListView<TControl>(IGraphics graphics, ControlStyle style)
    : UIControl(graphics, style, ISetting.None)
    where TControl : UIControl
{
    private readonly List<TControl> _rows = [];
    private int _selectedIndex;

    /// <summary>
    /// Gets or sets the gap between one row's top edge and the next.
    /// </summary>
    public float Spacing { get; set; }

    /// <summary>
    /// Gets or sets how many rows the window shows. Zero or fewer means no
    /// window: every row is drawn, which is a list that has been given no
    /// height to fit into rather than one with no room.
    /// </summary>
    public int VisibleCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the row the cursor is on is
    /// drawn selected. A read-only list still has a cursor - something has to
    /// decide which way the window moves - but showing it would offer the
    /// commander a choice the screen does not have.
    /// </summary>
    public bool ShowCursor { get; set; } = true;

    /// <summary>
    /// Gets the rows, in the order they are stacked.
    /// </summary>
    public IReadOnlyList<TControl> Rows => _rows;

    /// <summary>
    /// Gets or sets which row the cursor is on. Clamped to the rows that
    /// exist, so a caller may step it without bounds-checking first, and the
    /// window is moved to follow it.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => _selectedIndex = _rows.Count == 0 ? 0 : Math.Clamp(value, 0, _rows.Count - 1);
    }

    /// <summary>
    /// Gets the first row the window shows, which is derived from
    /// <see cref="SelectedIndex"/> rather than set.
    /// </summary>
    public int FirstVisible { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there are rows above the window.
    /// </summary>
    public bool HasRowsAbove => FirstVisible > 0;

    /// <summary>
    /// Gets a value indicating whether there are rows below the window.
    /// </summary>
    public bool HasRowsBelow => FirstVisible + Window < _rows.Count;

    private int Window => VisibleCount > 0 ? Math.Min(VisibleCount, _rows.Count) : _rows.Count;

    /// <summary>
    /// Adds a row to the bottom of the stack.
    /// </summary>
    /// <param name="row">The control to add.</param>
    public void Add(TControl row) => _rows.Add(row);

    /// <summary>
    /// Removes every row and puts the cursor back at the top.
    /// </summary>
    public void Clear()
    {
        _rows.Clear();
        _selectedIndex = 0;
        FirstVisible = 0;
    }

    /// <summary>
    /// Replaces every row, keeping the cursor where it was as far as the new
    /// rows allow. This is what a list whose contents change under it wants -
    /// an inventory gains and loses rows as cargo is traded - and it is why
    /// this is not <see cref="Clear"/> followed by <see cref="Add"/>: that
    /// would send the commander back to the top of the list on every change.
    /// </summary>
    /// <param name="rows">The rows to show, in order.</param>
    public void SetRows(IEnumerable<TControl> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        _rows.Clear();
        _rows.AddRange(rows);
        SelectedIndex = _selectedIndex;
    }

    /// <summary>
    /// Moves the cursor by one row, stopping at either end. Stopping rather
    /// than wrapping because a long list scrolled to its end is somewhere the
    /// commander walked to, and jumping back to the top would lose their place.
    /// </summary>
    /// <param name="delta">How far to move, in rows.</param>
    public void Move(int delta) => SelectedIndex = _selectedIndex + delta;

    /// <summary>
    /// Positions the rows the window shows, marks the selected one, and draws
    /// them. Rows outside the window are not positioned at all - a row that is
    /// not shown has nowhere to be.
    /// </summary>
    public override void Draw()
    {
        ScrollToSelection();

        int window = Window;
        float y = Position.Y;

        for (int i = FirstVisible; i < FirstVisible + window; i++)
        {
            TControl row = _rows[i];
            row.Position = new(Position.X, y);
            row.Width = Width;
            row.Height = Height;
            row.State = ShowCursor && i == _selectedIndex ? ControlState.Selected : ControlState.Normal;
            row.Draw();

            y += Spacing;
        }
    }

    // The least scrolling that puts the cursor back on screen.
    private void ScrollToSelection()
    {
        int window = Window;

        if (window == 0)
        {
            FirstVisible = 0;
            return;
        }

        if (_selectedIndex < FirstVisible)
        {
            FirstVisible = _selectedIndex;
        }
        else if (_selectedIndex >= FirstVisible + window)
        {
            FirstVisible = _selectedIndex - window + 1;
        }

        // Pins the last row to the bottom rather than leaving a gap if rows are removed under a scrolled window.
        FirstVisible = Math.Clamp(FirstVisible, 0, Math.Max(0, _rows.Count - window));
    }
}
