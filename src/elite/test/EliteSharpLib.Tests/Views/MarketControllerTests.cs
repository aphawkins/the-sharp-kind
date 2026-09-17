// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharpLib.Fakes;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;
using SharpKind.UI;

namespace EliteSharpLib.Tests.Views;

// The cursor is a 0-based row over the goods list; row 0 is a real good, never a None sentinel.
public class MarketControllerTests
{
    [Fact]
    public void ResetHighlightsTheFirstRow()
    {
        MarketController controller = CreateController(out _, out Trade trade);

        controller.Reset();

        Assert.Equal(0, controller.HighlightedRow);
        Assert.Equal("Food", controller.HighlightedStock.Good.Name);
        Assert.Equal(trade.StockMarket[0], controller.HighlightedStock);
    }

    [Fact]
    public void MovingUpAtTheFirstRowStaysThere()
    {
        MarketController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.UpArrow, default);
        controller.HandleInput();

        Assert.Equal(0, controller.HighlightedRow);
    }

    [Fact]
    public void MovingDownReachesTheLastRow()
    {
        MarketController controller = CreateController(out FakeKeyboard keyboard, out Trade trade);
        controller.Reset();

        // A press is read once, so each step down needs its own key-down.
        for (int i = 0; i < 20; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
        }

        Assert.Equal(trade.StockMarket.Count - 1, controller.HighlightedRow);
        Assert.Equal("Alien Items", controller.HighlightedStock.Good.Name);
    }

    [Fact]
    public void BuyingActsOnTheHighlightedGood()
    {
        MarketController controller = CreateController(out FakeKeyboard keyboard, out Trade trade);
        controller.Reset();
        trade.Credits = 1000;
        trade.StockMarket[0].CurrentPrice = 10;
        trade.StockMarket[0].CurrentQuantity = 5;

        keyboard.KeyDown(ConsoleKey.OemPeriod, default);
        controller.HandleInput();

        Assert.Equal(1, trade.StockMarket[0].CurrentCargo);
        Assert.Equal(4, trade.StockMarket[0].CurrentQuantity);
    }

    // The window is the rendition's answer to how many rows it has room for; a list that fits never scrolls.
    [Fact]
    public void AListThatFitsTheWindowNeverScrolls()
    {
        MarketController controller = CreateController(out FakeKeyboard keyboard, out _, visibleRows: 17);
        controller.Reset();

        for (int i = 0; i < 20; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
            controller.Draw();
        }

        Assert.Equal(0, controller.FirstVisibleRow);
    }

    // A list that does not fit scrolls exactly far enough to keep the cursor on screen.
    [Fact]
    public void ACursorLeavingASmallWindowScrollsIt()
    {
        MarketController controller = CreateController(out FakeKeyboard keyboard, out _, visibleRows: 5);
        controller.Reset();
        controller.Draw();

        Assert.Equal(0, controller.FirstVisibleRow);

        for (int i = 0; i < 6; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
            controller.Draw();
        }

        // Six steps down from row 0 puts the cursor on row 6, so a five-row
        // window has to start at row 2 to still show it.
        Assert.Equal(6, controller.HighlightedRow);
        Assert.Equal(2, controller.FirstVisibleRow);
    }

    private static MarketController CreateController(
        out FakeKeyboard keyboard,
        out Trade trade,
        int visibleRows = 17)
    {
        keyboard = new FakeKeyboard();
        ScreenManager<Screen, IScreenController> views = new(keyboard);
        GameState gameState = new(views, TestMissions.Registry());

        // A ship with no hold refuses every purchase, which a commander loaded
        // from a save never is: the standard bay is what Jameson launches with.
        PlayerShip ship = new(gameState) { CargoCapacity = 20 };
        trade = TestGoods.Trade(gameState, ship);
        FakeEliteDraw draw = new();

        return new MarketController(
            gameState,
            keyboard,
            trade,
            new PlanetController(gameState),
            new FakeBaseView(),
            draw,
            Style(visibleRows));
    }

    private static MarketListStyle Style(int visibleRows)
    {
        ControlColors nothing = new(default, default);
        ControlStyle style = new("Small", nothing, nothing, nothing);

        return new(
            RowStyle: style,
            HeadingStyle: style,
            Columns: [new(0, TextAlignment.Left)],
            Headings: [],
            RowsLeft: 0,
            FirstRowY: 0,
            RowHeight: 8,
            RowWidth: 100,
            VisibleRows: visibleRows,
            CashCaption: new("Cash:", 0, 0, TextAlignment.Left),
            CashAmount: new(string.Empty, 0, 0, TextAlignment.Right));
    }

    private sealed class FakeBaseView : IBaseView
    {
        public SharpKind.Graphics.IGraphics Graphics => throw new NotSupportedException();

        public ViewLayout Layout => throw new NotSupportedException();

        public void DrawFps(int fps)
        {
        }

        public void DrawHyperspaceCountdown(int countdown)
        {
        }

        public void DrawInfoMessage(string message)
        {
        }

        public void DrawTextPretty(System.Numerics.Vector2 position, float width, string text)
        {
        }

        public void DrawViewHeader(string title)
        {
        }
    }
}
