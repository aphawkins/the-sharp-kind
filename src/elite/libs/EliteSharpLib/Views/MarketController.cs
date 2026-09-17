// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Globalization;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Trader;
using SharpKind.Input;
using SharpKind.UI;

namespace EliteSharpLib.Views;

/// <summary>
/// The market screen: a scrolling list of the goods being traded, the cursor
/// over it, and buying or selling whichever good it is on.
/// <para>
/// The rows are controls rather than a model handed to a per-rendition view.
/// The settings screens went this way first, and the market has a second reason
/// the settings did not: the goods are a plugin, so how many rows there are is
/// not something a rendition can be written against. It declares how many it
/// has room for - <see cref="MarketListStyle.VisibleRows"/> - and the list
/// scrolls, which is what lets a goods set larger than the classic seventeen be
/// traded at all.
/// </para>
/// </summary>
internal sealed class MarketController : IScreenController
{
    private readonly GameState _gameState;
    private readonly IKeyboard _keyboard;
    private readonly PlanetController _planet;
    private readonly Trade _trade;
    private readonly IBaseView _baseView;
    private readonly IViewSurface _surface;
    private readonly MarketListStyle _style;
    private readonly ListView<TableRow> _rows;
    private readonly Label _cashAmount;

    internal MarketController(
        GameState gameState,
        IKeyboard keyboard,
        Trade trade,
        PlanetController planet,
        IBaseView baseView,
        IViewSurface surface,
        MarketListStyle style)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(style);

        _gameState = gameState;
        _keyboard = keyboard;
        _trade = trade;
        _planet = planet;
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
        };

        foreach (StockItem stock in trade.StockMarket)
        {
            _rows.Add(new TableRow(surface.Graphics, style.RowStyle, style.Columns, new MarketRowCells(stock)));
        }

        // A label, not a caption: it's the one part of the cash line that changes.
        _cashAmount = new(surface.Graphics, style.RowStyle, new CreditsText(trade))
        {
            Alignment = TextAlignment.Right,
            Position = new(style.CashAmount.X, style.CashAmount.Y),
        };
    }

    // Exposed for tests: which row the cursor is on, and the good it names.
    internal int HighlightedRow => _rows.SelectedIndex;

    internal StockItem HighlightedStock => _trade.StockMarket[_rows.SelectedIndex];

    internal int FirstVisibleRow => _rows.FirstVisible;

    public void Draw()
    {
        _baseView.DrawViewHeader($"{_planet.NamePlanet(_gameState.DockedPlanet)} MARKET PRICES");

        foreach (PlacedText heading in _style.Headings)
        {
            DrawPlaced(heading, _style.HeadingStyle);
        }

        _rows.Draw();

        DrawPlaced(_style.CashCaption, _style.HeadingStyle);

        // Right-aligned against its own position: a zero-width box ends where it starts.
        _cashAmount.Draw();
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

        if (_keyboard.IsPressed(ConsoleKey.OemComma) || _keyboard.IsPressed(ConsoleKey.LeftArrow))
        {
            _trade.SellStock(HighlightedStock);
        }

        if (_keyboard.IsPressed(ConsoleKey.OemPeriod) || _keyboard.IsPressed(ConsoleKey.RightArrow))
        {
            _trade.BuyStock(HighlightedStock);
        }
    }

    public void Reset() => _rows.SelectedIndex = 0;

    public void Update()
    {
    }

    private void DrawPlaced(in PlacedText placed, ControlStyle style)
    {
        if (placed.Text.Length == 0)
        {
            return;
        }

        ControlColors colors = style.Colors(ControlState.Normal);

        if (placed.Alignment == TextAlignment.Right)
        {
            _surface.Graphics.DrawTextRight(new(placed.X, placed.Y), placed.Text, style.FontType, colors.Text);
            return;
        }

        _surface.Graphics.DrawTextLeft(new(placed.X, placed.Y), placed.Text, style.FontType, colors.Text);
    }

    /// <summary>
    /// One goods row seen as cells, in the order
    /// <see cref="MarketListStyle.Columns"/> declares: the name, the unit, the
    /// price, the quantity for sale with its unit, and the quantity in the hold
    /// with its unit. Read from the market at every draw, so a row cannot be
    /// left showing a price the market has moved off.
    /// </summary>
    private sealed class MarketRowCells(StockItem stock) : IRowCells
    {
        public IReadOnlyList<string> Cells => BuildCells();

        // A quantity of nothing is a dash with no unit: "-t" would read as a tonne of something.
        private string[] BuildCells()
        {
            string units = stock.Good.Units;
            int forSale = stock.CurrentQuantity;
            int inHold = stock.CurrentCargo;

            return
            [
                stock.Good.Name,
                units,
                stock.CurrentPrice.ToString("N1", CultureInfo.CurrentCulture),
                forSale > 0 ? forSale.ToString(CultureInfo.CurrentCulture) : "-",
                forSale > 0 ? units : string.Empty,
                inHold > 0 ? inHold.ToString(CultureInfo.CurrentCulture) : "-",
                inHold > 0 ? units : string.Empty,
            ];
        }
    }

    // A binding rather than a string, so the label can't be left stale after a trade.
    private sealed class CreditsText(Trade trade) : ISetting
    {
        public string Name => $"{trade.Credits:N1} Credits";

        public IReadOnlyList<string> Values => [];

        public int SelectedIndex { get; set; }
    }
}
