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
using SharpKind.Graphics.Fakes;
using SharpKind.UI;

namespace EliteSharpLib.Tests.Views;

// The inventory lists only what is aboard, so its rows come and go as cargo is
// traded. It scrolls but shows no cursor: nothing on this screen can be chosen,
// and a hold can carry more sorts of thing than the tier has rows for.
public class InventoryControllerTests
{
    [Fact]
    public void OnlyCargoAboardIsListed()
    {
        InventoryController controller = CreateController(out _, out Trade trade, out _);
        trade["Food"].CurrentCargo = 3;
        trade["Gold"].CurrentCargo = 1;

        controller.Reset();

        Assert.Equal(2, controller.CarriedRowCount);
    }

    [Fact]
    public void AnEmptyHoldListsNothing()
    {
        InventoryController controller = CreateController(out _, out _, out _);

        controller.Reset();

        Assert.Equal(0, controller.CarriedRowCount);
    }

    [Fact]
    public void SellingTheLastOfAGoodDropsItsRow()
    {
        InventoryController controller = CreateController(out _, out Trade trade, out _);
        trade["Food"].CurrentCargo = 1;
        controller.Reset();
        Assert.Equal(1, controller.CarriedRowCount);

        trade["Food"].CurrentCargo = 0;
        controller.Update();

        Assert.Equal(0, controller.CarriedRowCount);
    }

    // A hold with more sorts of thing in it than the tier has rows for used to
    // draw off the bottom of the screen and into the HUD.
    [Fact]
    public void AFullHoldScrollsRatherThanRunningOffTheScreen()
    {
        InventoryController controller = CreateController(out FakeKeyboard keyboard, out Trade trade, out _, visibleRows: 5);
        foreach (StockItem stock in trade.StockMarket)
        {
            stock.CurrentCargo = 1;
        }

        controller.Reset();
        controller.Draw();
        Assert.Equal(trade.StockMarket.Count, controller.CarriedRowCount);
        Assert.Equal(0, controller.FirstVisibleRow);

        for (int i = 0; i < 6; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
            controller.Draw();
        }

        Assert.Equal(2, controller.FirstVisibleRow);
    }

    // The reason the list is replaced rather than cleared and refilled: a trade
    // must not send the commander back to the top of a scrolled list.
    [Fact]
    public void ATradeKeepsTheScrollPosition()
    {
        InventoryController controller = CreateController(out FakeKeyboard keyboard, out Trade trade, out _, visibleRows: 5);
        foreach (StockItem stock in trade.StockMarket)
        {
            stock.CurrentCargo = 1;
        }

        controller.Reset();
        for (int i = 0; i < 8; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
            controller.Draw();
        }

        int scrolled = controller.FirstVisibleRow;
        Assert.True(scrolled > 0);

        // Something at the far end of the list is sold off.
        trade.StockMarket[^1].CurrentCargo = 0;
        controller.Update();
        controller.Draw();

        Assert.Equal(scrolled, controller.FirstVisibleRow);
    }

    // Nothing here can be chosen, so no row is ever drawn highlighted - the
    // cursor exists only to move the window.
    [Fact]
    public void NoRowIsDrawnWithACursorBlock()
    {
        InventoryController controller = CreateController(out _, out Trade trade, out RecordingGraphics graphics);
        trade["Food"].CurrentCargo = 1;
        trade["Gold"].CurrentCargo = 1;
        controller.Reset();

        controller.Draw();

        Assert.Empty(graphics.FilledRectangles);
    }

    private static InventoryController CreateController(
        out FakeKeyboard keyboard,
        out Trade trade,
        out RecordingGraphics graphics,
        int visibleRows = 17)
    {
        keyboard = new FakeKeyboard();
        ScreenManager<Screen, IScreenController> views = new(keyboard);
        GameState gameState = new(views, TestMissions.Registry());
        PlayerShip ship = new(gameState) { CargoCapacity = 35 };
        trade = TestGoods.Trade(gameState, ship);
        graphics = new RecordingGraphics();
        FakeEliteDraw draw = new() { Graphics = graphics };

        return new InventoryController(ship, trade, keyboard, new FakeBaseView(), draw, Style(visibleRows));
    }

    private static InventoryListStyle Style(int visibleRows)
    {
        ControlColors nothing = ControlColors.TextOnly(default);
        ControlStyle style = new("Small", nothing, nothing, nothing);

        return new(
            RowStyle: style,
            CaptionStyle: style,
            Columns: [new(0, TextAlignment.Left), new(80, TextAlignment.Left)],
            FuelCaption: new("Fuel:", 0, 0, TextAlignment.Left),
            FuelValue: new(string.Empty, 40, 0, TextAlignment.Left),
            CashCaption: new("Cash:", 0, 8, TextAlignment.Left),
            CashValue: new(string.Empty, 40, 8, TextAlignment.Left),
            RowsLeft: 0,
            FirstRowY: 24,
            RowHeight: 8,
            RowWidth: 200,
            VisibleRows: visibleRows);
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
