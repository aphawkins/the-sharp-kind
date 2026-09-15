// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Globalization;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Ships;
using EliteSharpLib.Trader;
using SharpKind.Input;
using SharpKind.UI;

namespace EliteSharpLib.Views;

/// <summary>
/// The inventory screen: what the ship is carrying, over as many rows as the
/// tier has room for and scrolling past that.
/// <para>
/// The rows are controls rather than a model handed to a per-rendition view,
/// as the market's are. A hold can carry more sorts of thing than the screen
/// has rows - the goods are a plugin, so how many sorts there are is not fixed
/// - and the old screen simply drew off the bottom when it did.
/// </para>
/// <para>
/// Only what is aboard is listed, so the rows change as cargo is traded. One
/// row per good is built once and the list is given whichever of them are
/// carried, which is what keeps the scroll position across a trade.
/// </para>
/// </summary>
internal sealed class InventoryController : IScreenController
{
    private readonly Trade _trade;
    private readonly IKeyboard _keyboard;
    private readonly IBaseView _baseView;
    private readonly IViewSurface _surface;
    private readonly InventoryListStyle _style;
    private readonly Dictionary<StockItem, TableRow> _rowFor = [];
    private readonly List<TableRow> _carried = [];
    private readonly ListView<TableRow> _rows;
    private readonly Label _fuel;
    private readonly Label _cash;

    internal InventoryController(
        PlayerShip ship,
        Trade trade,
        IKeyboard keyboard,
        IBaseView baseView,
        IViewSurface surface,
        InventoryListStyle style)
    {
        ArgumentNullException.ThrowIfNull(trade);
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(style);

        _trade = trade;
        _keyboard = keyboard;
        _baseView = baseView;
        _surface = surface;
        _style = style;

        _rows = new(surface.Graphics, style.RowStyle)
        {
            Position = new(style.RowsLeft, style.FirstRowY),
            Width = style.RowWidth,
            Height = style.RowHeight,
            Spacing = style.RowHeight,
            VisibleCount = style.VisibleRows,

            // Nothing on this screen can be chosen, so the cursor that moves
            // the window is not drawn.
            ShowCursor = false,
        };

        foreach (StockItem stock in trade.StockMarket)
        {
            _rowFor[stock] = new TableRow(surface.Graphics, style.RowStyle, style.Columns, new CargoRowCells(stock));
        }

        _fuel = Value(surface, style, style.FuelValue, new FuelText(ship));
        _cash = Value(surface, style, style.CashValue, new CreditsText(trade));
    }

    // Exposed for tests: what the list is showing, and where its window is.
    internal int CarriedRowCount => _rows.Rows.Count;

    internal int FirstVisibleRow => _rows.FirstVisible;

    public void Draw()
    {
        _baseView.DrawViewHeader("INVENTORY");

        DrawPlaced(_style.FuelCaption, _style.CaptionStyle);
        DrawPlaced(_style.CashCaption, _style.CaptionStyle);
        _fuel.Draw();
        _cash.Draw();

        _rows.Draw();
    }

    public void HandleInput()
    {
        if (_keyboard.IsPressed(ConsoleKey.S) || _keyboard.IsPressed(ConsoleKey.UpArrow))
        {
            _rows.Move(-1);
        }

        if (_keyboard.IsPressed(ConsoleKey.X) || _keyboard.IsPressed(ConsoleKey.DownArrow))
        {
            _rows.Move(1);
        }
    }

    public void Reset()
    {
        RefreshCarried();
        _rows.SelectedIndex = 0;
    }

    public void Update() => RefreshCarried();

    private static Label Value(IViewSurface surface, InventoryListStyle style, in PlacedText placed, ISetting setting)
        => new(surface.Graphics, style.RowStyle, setting)
        {
            Alignment = placed.Alignment,
            Position = new(placed.X, placed.Y),
        };

    // Only what is aboard appears. The rows themselves are built once, so this
    // reorders an existing list rather than making controls every tick.
    private void RefreshCarried()
    {
        _carried.Clear();

        foreach (StockItem stock in _trade.StockMarket)
        {
            if (stock.CurrentCargo > 0)
            {
                _carried.Add(_rowFor[stock]);
            }
        }

        _rows.SetRows(_carried);
    }

    private void DrawPlaced(in PlacedText placed, ControlStyle style)
    {
        if (placed.Text.Length == 0)
        {
            return;
        }

        ControlColors colors = style.Colors(ControlState.Normal);
        _surface.Graphics.DrawTextLeft(new(placed.X, placed.Y), placed.Text, style.FontType, colors.Text);
    }

    /// <summary>
    /// One cargo row seen as cells, in the order
    /// <see cref="InventoryListStyle.Columns"/> declares: what is carried, and
    /// how much of it with its unit. Read from the hold at every draw.
    /// </summary>
    private sealed class CargoRowCells(StockItem stock) : IRowCells
    {
        public IReadOnlyList<string> Cells => BuildCells();

        private string[] BuildCells()
        {
            string quantity = stock.CurrentCargo.ToString(CultureInfo.CurrentCulture);

            return [stock.Good.Name, quantity + stock.Good.Units];
        }
    }

    private sealed class FuelText(PlayerShip ship) : ISetting
    {
        public string Name => $"{ship.Fuel.ToString("N1", CultureInfo.CurrentCulture)} Light Years";

        public IReadOnlyList<string> Values => [];

        public int SelectedIndex { get; set; }
    }

    private sealed class CreditsText(Trade trade) : ISetting
    {
        public string Name => $"{trade.Credits.ToString("N1", CultureInfo.CurrentCulture)} Credits";

        public IReadOnlyList<string> Values => [];

        public int SelectedIndex { get; set; }
    }
}
