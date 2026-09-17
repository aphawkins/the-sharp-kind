// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Ships;
using EliteSharp.Abstractions.Views;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Conflict;
using EliteSharpLib.Equipment;
using EliteSharpLib.Fakes;
using EliteSharpLib.Lasers;
using EliteSharpLib.Missions;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Fakes;
using SharpKind.Fakes.Audio;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Views;

// The equip-ship list's visibility and cursor: collapsed laser categories never reach the model at all.
public class EquipmentControllerTests
{
    private static readonly string[] s_mounts = ["Front", "Rear", "Left", "Right"];

    [Fact]
    public void ResetShowsOnlyTopLevelRows()
    {
        EquipmentController controller = CreateController(out GameState gameState, out _);
        gameState.CurrentPlanetData.TechLevel = 10;

        controller.Reset();

        EquipmentModel model = controller.BuildModel();
        Assert.All(model.Rows, row => Assert.False(row.IsIndented));
        Assert.Contains(model.Rows, row => row.Name == "Pulse Laser");
    }

    [Fact]
    public void BuyingALaserCategoryRevealsItsMountChoices()
    {
        EquipmentController controller = CreateController(out GameState gameState, out _);
        gameState.CurrentPlanetData.TechLevel = 10;
        controller.Reset();

        // Ten top-level rows (Fuel..Galactic Hyperdrive) precede Pulse Laser in the stock array.
        for (int i = 0; i < 10; i++)
        {
            controller.SelectNext();
        }

        controller.Buy();

        // ListPrices resets the cursor to row zero as a side effect of expanding a category.
        EquipmentModel model = controller.BuildModel();
        Assert.Contains(model.Rows, row => row.Name == "Front" && row.IsIndented);
        Assert.True(model.Rows[0].IsHighlighted);
    }

    [Fact]
    public void APriceThatTruncatesToZeroIsNotShown()
    {
        EquipmentController controller = CreateController(out GameState gameState, out PlayerShip ship);
        gameState.CurrentPlanetData.TechLevel = 10;
        ship.Fuel = 6.9f; // (7 - 6.9) * 2 = 0.2, truncates to zero.

        controller.Reset();

        Assert.Equal(string.Empty, controller.BuildModel().Rows[0].Price);
    }

    [Fact]
    public void ANonZeroPriceIsFormattedToOneDecimalPlace()
    {
        EquipmentController controller = CreateController(out GameState gameState, out PlayerShip ship);
        gameState.CurrentPlanetData.TechLevel = 10;
        ship.Fuel = 0;

        controller.Reset();

        Assert.Equal("14.0", controller.BuildModel().Rows[0].Price);
    }

    [Fact]
    public void DockingComputerCosts1000Credits()
    {
        EquipmentController controller = CreateController(out GameState gameState, out _);
        gameState.CurrentPlanetData.TechLevel = 10;

        controller.Reset();

        EquipmentModel model = controller.BuildModel();
        Assert.Equal("1,000.0", Assert.Single(model.Rows, row => row.Name == "Docking Computers").Price);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    public void MilitaryLaserRequiresPlanetTechLevel10(int planetTechLevel, bool expectedVisible)
    {
        // Original PRXS position 12 (0-based): shown once planet tech + 3 > 12, i.e. tech >= 10.
        EquipmentController controller = CreateController(out GameState gameState, out _);
        gameState.CurrentPlanetData.TechLevel = planetTechLevel;

        controller.Reset();

        EquipmentModel model = controller.BuildModel();
        Assert.Equal(expectedVisible, model.Rows.Any(row => row.Name == "Military Laser"));
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public void MiningLaserRequiresPlanetTechLevel11(int planetTechLevel, bool expectedVisible)
    {
        // Original PRXS position 13 (0-based): shown once planet tech + 3 > 13, i.e. tech >= 11.
        EquipmentController controller = CreateController(out GameState gameState, out _);
        gameState.CurrentPlanetData.TechLevel = planetTechLevel;

        controller.Reset();

        EquipmentModel model = controller.BuildModel();
        Assert.Equal(expectedVisible, model.Rows.Any(row => row.Name == "Mining Laser"));
    }

    [Theory]
    [InlineData(ConsoleKey.X, 1)]
    [InlineData(ConsoleKey.DownArrow, 1)]
    [InlineData(ConsoleKey.S, 0)]
    [InlineData(ConsoleKey.UpArrow, 0)]
    public void TheCursorKeysAndTheirAliasesMoveTheCursor(ConsoleKey key, int expectedRow)
    {
        EquipmentController controller = CreateController(out GameState gameState, out _, out _, out FakeKeyboard keyboard);
        gameState.CurrentPlanetData.TechLevel = 10;
        controller.Reset();

        keyboard.KeyDown(key, default);
        controller.HandleInput();

        Assert.True(controller.BuildModel().Rows[expectedRow].IsHighlighted);
    }

    [Fact]
    public void TheCursorStopsAtBothEndsOfTheList()
    {
        EquipmentController controller = CreateController(out GameState gameState, out _, out _);
        gameState.CurrentPlanetData.TechLevel = 10;
        controller.Reset();

        // Already on the first row, so there is nothing above it.
        controller.SelectPrevious();
        Assert.True(controller.BuildModel().Rows[0].IsHighlighted);

        // Far more steps than the list has rows: it must stop at the last row.
        for (int i = 0; i < 60; i++)
        {
            controller.SelectNext();
        }

        Assert.True(controller.BuildModel().Rows[^1].IsHighlighted);

        controller.SelectNext();
        Assert.True(controller.BuildModel().Rows[^1].IsHighlighted);
    }

    [Fact]
    public void TheCursorWalksBackUpTheListAndStopsAtTheLastRow()
    {
        EquipmentController controller = CreateController(out GameState gameState, out _, out Trade trade);
        gameState.CurrentPlanetData.TechLevel = 12;
        trade.Credits = 20000;
        controller.Reset();

        // Military Right is the last row in the stock list; opening that category puts the cursor on it.
        SelectRow(controller, "Military Laser");
        controller.Buy();
        SelectRow(controller, "Right");

        controller.SelectNext();
        Assert.True(controller.BuildModel().Rows[^1].IsHighlighted);

        controller.SelectPrevious();
        Assert.True(controller.BuildModel().Rows[^2].IsHighlighted);
    }

    // Nothing on this screen moves on its own.
    [Fact]
    public void AnIdleScreenDoesNotChange()
    {
        EquipmentController controller = CreateController(out GameState gameState, out _, out _);
        gameState.CurrentPlanetData.TechLevel = 10;
        controller.Reset();
        EquipmentModel before = controller.BuildModel();

        controller.Update();

        Assert.Equal(before.Rows, controller.BuildModel().Rows);
    }

    [Fact]
    public void TheEnterKeyBuysWhateverTheCursorIsOn()
    {
        EquipmentController controller = CreateController(
            out GameState gameState, out PlayerShip ship, out Trade trade, out FakeKeyboard keyboard);
        gameState.CurrentPlanetData.TechLevel = 10;
        trade.Credits = 10000;
        ship.Fuel = 0;
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(ship.MaxFuel, ship.Fuel);
    }

    // One row per branch of the buy switch. Fuel and missiles are capped rather than owned, so have tests of their own below.
    [Theory]
    [InlineData("Large Cargo Bay", 400)]
    [InlineData("E.C.M. System", 600)]
    [InlineData("Fuel Scoops", 525)]
    [InlineData("Escape Capsule", 1000)]
    [InlineData("Energy Bomb", 900)]
    [InlineData("Extra Energy Unit", 1500)]
    [InlineData("Docking Computers", 1000)]
    [InlineData("Galactic Hyperdrive", 5000)]
    public void BuyingAPieceOfEquipmentFitsItAndTakesItsPrice(string name, float price)
    {
        EquipmentController controller = CreateController(out GameState gameState, out PlayerShip ship, out Trade trade);
        gameState.CurrentPlanetData.TechLevel = 10;
        trade.Credits = 10000;
        controller.Reset();
        SelectRow(controller, name);

        controller.Buy();

        Assert.Equal(10000 - price, trade.Credits);
        Assert.True(IsFitted(ship, name));

        // Fitted equipment cannot be bought twice, so the row stops offering.
        Assert.False(Assert.Single(controller.BuildModel().Rows, row => row.Name == name).IsAffordable);
    }

    [Fact]
    public void BuyingFuelFillsTheTankAndCostsTwoCreditsALightYear()
    {
        EquipmentController controller = CreateController(out GameState gameState, out PlayerShip ship, out Trade trade);
        gameState.CurrentPlanetData.TechLevel = 10;
        trade.Credits = 100;
        ship.Fuel = 4;
        controller.Reset();

        controller.Buy();

        Assert.Equal(ship.MaxFuel, ship.Fuel);
        Assert.Equal(100 - ((7 - 4) * 2), trade.Credits);
    }

    [Fact]
    public void MissilesCanBeBoughtUpToFourAndNoFurther()
    {
        EquipmentController controller = CreateController(out GameState gameState, out PlayerShip ship, out Trade trade);
        gameState.CurrentPlanetData.TechLevel = 10;
        trade.Credits = 10000;
        ship.MissileCount = 0;
        controller.Reset();

        for (int i = 0; i < 4; i++)
        {
            SelectRow(controller, "Missile");
            controller.Buy();
        }

        Assert.Equal(4, ship.MissileCount);

        // The fourth is the last: a full rack counts as fitted equipment.
        SelectRow(controller, "Missile");
        controller.Buy();

        Assert.Equal(4, ship.MissileCount);
    }

    [Fact]
    public void NothingIsBoughtWithoutTheCreditsToPayForIt()
    {
        EquipmentController controller = CreateController(out GameState gameState, out PlayerShip ship, out Trade trade);
        gameState.CurrentPlanetData.TechLevel = 10;
        trade.Credits = 999;
        controller.Reset();
        SelectRow(controller, "Docking Computers");

        controller.Buy();

        Assert.False(ship.HasDockingComputer);
        Assert.Equal(999, trade.Credits);
    }

    // Every laser on every mount: it must land on the bought mount and nowhere else.
    [Theory]
    [InlineData("Pulse Laser", "Front", LaserType.Pulse)]
    [InlineData("Pulse Laser", "Rear", LaserType.Pulse)]
    [InlineData("Pulse Laser", "Left", LaserType.Pulse)]
    [InlineData("Pulse Laser", "Right", LaserType.Pulse)]
    [InlineData("Beam Laser", "Front", LaserType.Beam)]
    [InlineData("Beam Laser", "Rear", LaserType.Beam)]
    [InlineData("Beam Laser", "Left", LaserType.Beam)]
    [InlineData("Beam Laser", "Right", LaserType.Beam)]
    [InlineData("Mining Laser", "Front", LaserType.Mining)]
    [InlineData("Mining Laser", "Rear", LaserType.Mining)]
    [InlineData("Mining Laser", "Left", LaserType.Mining)]
    [InlineData("Mining Laser", "Right", LaserType.Mining)]
    [InlineData("Military Laser", "Front", LaserType.Military)]
    [InlineData("Military Laser", "Rear", LaserType.Military)]
    [InlineData("Military Laser", "Left", LaserType.Military)]
    [InlineData("Military Laser", "Right", LaserType.Military)]
    public void ALaserIsFittedToTheMountItWasBoughtFor(string category, string mount, LaserType expected)
    {
        EquipmentController controller = CreateController(out GameState gameState, out PlayerShip ship, out Trade trade);
        gameState.CurrentPlanetData.TechLevel = 12;
        trade.Credits = 20000;
        controller.Reset();

        SelectRow(controller, category);
        controller.Buy();
        SelectRow(controller, mount);
        controller.Buy();

        Assert.Equal(expected, LaserOn(ship, mount).Type);
        foreach (string other in s_mounts.Where(m => m != mount))
        {
            Assert.Equal(LaserType.None, LaserOn(ship, other).Type);
        }
    }

    // The laser being replaced is traded in, at what it was worth.
    [Theory]
    [InlineData("Pulse Laser", 400)]
    [InlineData("Beam Laser", 1000)]
    [InlineData("Mining Laser", 800)]
    [InlineData("Military Laser", 6000)]
    public void ReplacingALaserRefundsWhatTheOldOneWasWorth(string category, float refund)
    {
        EquipmentController controller = CreateController(out GameState gameState, out _, out Trade trade);
        gameState.CurrentPlanetData.TechLevel = 12;
        trade.Credits = 20000;
        controller.Reset();

        // Fit the laser under test, then fit a different one over it.
        SelectRow(controller, category);
        controller.Buy();
        SelectRow(controller, "Front");
        controller.Buy();
        float afterFirst = trade.Credits;

        string replacement = category == "Pulse Laser" ? "Beam Laser" : "Pulse Laser";
        float replacementPrice = replacement == "Pulse Laser" ? 400 : 1000;
        SelectRow(controller, replacement);
        controller.Buy();
        SelectRow(controller, "Front");
        controller.Buy();

        Assert.Equal(afterFirst - replacementPrice + refund, trade.Credits);
    }

    [Fact]
    public void ExpandingACategoryHidesEveryOtherCategorysMounts()
    {
        EquipmentController controller = CreateController(out GameState gameState, out _, out Trade trade);
        gameState.CurrentPlanetData.TechLevel = 12;
        trade.Credits = 20000;
        controller.Reset();

        SelectRow(controller, "Pulse Laser");
        controller.Buy();
        SelectRow(controller, "Beam Laser");
        controller.Buy();

        // Only one set of mounts is ever on screen, however many categories have been opened.
        Assert.Equal(1, controller.BuildModel().Rows.Count(row => row.Name == "Front"));
    }

    [Fact]
    public void TheCashLineIsThePlayersCredits()
    {
        EquipmentController controller = CreateController(out GameState gameState, out _, out Trade trade);
        gameState.CurrentPlanetData.TechLevel = 10;
        trade.Credits = 1234.5f;

        controller.Reset();

        Assert.Equal("Cash: 1,234.5 Credits", controller.BuildModel().Cash);
    }

    [Fact]
    public void DrawHandsTheViewTheModelItWouldHaveBuilt()
    {
        EquipmentController controller = CreateController(
            out GameState gameState, out _, out _, out _, out FakeEquipmentView view);
        gameState.CurrentPlanetData.TechLevel = 10;
        controller.Reset();

        controller.Draw();

        // The model holds a list, so a record comparison is by reference; compare its contents instead.
        Assert.Equal(controller.BuildModel().Rows, view.Drawn?.Rows);
        Assert.Equal(controller.BuildModel().Cash, view.Drawn?.Cash);
    }

    private static ILaser LaserOn(PlayerShip ship, string mount) => mount switch
    {
        "Front" => ship.LaserFront,
        "Rear" => ship.LaserRear,
        "Left" => ship.LaserLeft,
        "Right" => ship.LaserRight,
        _ => throw new ArgumentOutOfRangeException(nameof(mount)),
    };

    private static bool IsFitted(PlayerShip ship, string name) => name switch
    {
        "Large Cargo Bay" => ship.CargoCapacity == 35,
        "E.C.M. System" => ship.HasECM,
        "Fuel Scoops" => ship.HasFuelScoop,
        "Escape Capsule" => ship.HasEscapeCapsule,
        "Energy Bomb" => ship.HasEnergyBomb,
        "Extra Energy Unit" => ship.EnergyUnit == EnergyUnit.Extra,
        "Docking Computers" => ship.HasDockingComputer,
        "Galactic Hyperdrive" => ship.HasGalacticHyperdrive,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    // Walks the cursor down to the named row; a bounded walk either lands on it or shows it is not on screen.
    private static void SelectRow(EquipmentController controller, string name)
    {
        for (int i = 0; i < 40; i++)
        {
            if (controller.BuildModel().Rows.Any(row => row.IsHighlighted && row.Name == name))
            {
                return;
            }

            controller.SelectNext();
        }

        Assert.Fail(name + " is not on the list");
    }

    private static EquipmentController CreateController(out GameState gameState, out PlayerShip ship)
        => Build(out gameState, out ship, out _, out _, out _);

    private static EquipmentController CreateController(
        out GameState gameState, out PlayerShip ship, out Trade trade)
        => Build(out gameState, out ship, out trade, out _, out _);

    private static EquipmentController CreateController(
        out GameState gameState, out PlayerShip ship, out Trade trade, out FakeKeyboard keyboard)
        => Build(out gameState, out ship, out trade, out keyboard, out _);

    private static EquipmentController CreateController(
        out GameState gameState,
        out PlayerShip ship,
        out Trade trade,
        out FakeKeyboard keyboard,
        out FakeEquipmentView view)
        => Build(out gameState, out ship, out trade, out keyboard, out view);

    private static EquipmentController Build(
        out GameState gameState,
        out PlayerShip ship,
        out Trade trade,
        out FakeKeyboard keyboard,
        out FakeEquipmentView view)
    {
        keyboard = new FakeKeyboard();
        ScreenManager<Screen, IScreenController> views = new(keyboard);
        gameState = new(views, TestMissions.Registry());
        ship = new PlayerShip(gameState);
        trade = TestGoods.Trade(gameState, ship);
        view = new FakeEquipmentView();
        FakeEliteDraw draw = new();
        RNG rng = new(new FakeRandomSource());
        FakeShipFactory shipFactory = new(draw);
        Universe universe = new(shipFactory, rng);
        AudioController audio = new(new FakeSound(), new Dictionary<string, SfxSample>(), new());
        Pilot pilot = new(draw, audio, universe, ship, gameState);
        MissionRunner missions = TestMissions.Runner(gameState, ship, trade);

        Combat combat = new(
            gameState,
            audio,
            ship,
            trade,
            pilot,
            universe,
            draw,
            new SixteenBitRendition(),
            shipFactory,
            rng,
            missions);
        ScannerController scanner = new(gameState, ship, universe, combat, new NothingScannerView());

        return new EquipmentController(gameState, keyboard, ship, trade, scanner, view);
    }

    // Buying fuel or a missile refreshes the HUD; what it draws is not under
    // test here.
    private sealed class NothingScannerView : IView<ScannerModel>
    {
        public void Draw(ScannerModel model)
        {
        }
    }

    // Records rather than draws: what reaches the view is all Draw does.
    private sealed class FakeEquipmentView : IView<EquipmentModel>
    {
        public EquipmentModel? Drawn { get; private set; }

        public void Draw(EquipmentModel model) => Drawn = model;
    }
}
