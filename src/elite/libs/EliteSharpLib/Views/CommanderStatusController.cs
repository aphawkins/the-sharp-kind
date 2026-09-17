// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Ships;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Equipment;
using EliteSharpLib.Ships;
using EliteSharpLib.Trader;
using EliteSharpLib.Types;

namespace EliteSharpLib.Views;

/// <summary>
/// Everything the commander status screen derives: the combat rating, the
/// condition, and the list of equipment fitted.
/// </summary>
internal sealed class CommanderStatusController : IScreenController
{
    private static readonly string[] s_conditionText =
    [
        "Docked",
        "Green",
        "Yellow",
        "Red",
    ];

    private static readonly (int Score, string Title)[] s_ratings =
    [
        new(0x0000, "Harmless"),
        new(0x0008, "Mostly Harmless"),
        new(0x0010, "Poor"),
        new(0x0020, "Average"),
        new(0x0040, "Above Average"),
        new(0x0080, "Competent"),
        new(0x0200, "Dangerous"),
        new(0x0A00, "Deadly"),
        new(0x1900, "---- E L I T E ----"),
    ];

    private readonly GameState _gameState;
    private readonly PlanetController _planet;
    private readonly PlayerShip _ship;
    private readonly Trade _trade;
    private readonly Universe _universe;
    private readonly IView<CommanderStatusModel> _view;

    internal CommanderStatusController(
        GameState gameState,
        PlayerShip ship,
        Trade trade,
        PlanetController planet,
        Universe universe,
        IView<CommanderStatusModel> view)
    {
        _gameState = gameState;
        _ship = ship;
        _trade = trade;
        _planet = planet;
        _universe = universe;
        _view = view;
    }

    public void Draw() => _view.Draw(BuildModel());

    public void HandleInput()
    {
    }

    public void Reset()
    {
    }

    public void Update()
    {
    }

    // Exposed for tests: the whole screen as formatted text.
    internal CommanderStatusModel BuildModel()
    {
        // In witchspace there is no present system to name.
        string presentSystem = _gameState.InWitchspace
            ? string.Empty
            : _planet.NamePlanet(_gameState.DockedPlanet).CapitaliseFirstLetter();

        return new(
            $"COMMANDER {_gameState.Cmdr.Name}",
            presentSystem,
            _planet.NamePlanet(_gameState.HyperspacePlanet).CapitaliseFirstLetter(),
            s_conditionText[CurrentCondition()],
            $"{_ship.Fuel:N1} Light Years",
            $"{_trade.Credits:N1} Credits",
            LegalStatusBand.For(_gameState.Cmdr.LegalStatus),
            CurrentRating(),
            [.. EquipmentFitted()]);
    }

    // The commander's combat rating, the highest one their score has reached.
    private string CurrentRating()
    {
        string rating = string.Empty;
        foreach ((int score, string title) in s_ratings)
        {
            if (_gameState.Cmdr.Score >= score)
            {
                rating = title;
            }
        }

        return rating;
    }

    // Docked / Green / Yellow / Red, as an index into s_conditionText.
    private int CurrentCondition()
    {
        if (_gameState.IsDocked)
        {
            return 0;
        }

        int condition = 1;

        foreach (IObject obj in _universe.GetAllObjects())
        {
            // Matches the original: a missile, or above a Rock Splinter but below the Dodec.
            if (obj.Id == ObjectIds.Missile
                || (obj.Traits.HasFlag(ShipTraits.Crewed) && !obj.Flags.HasFlag(ShipProperties.Station)))
            {
                condition = 2;
                break;
            }
        }

        if (condition == 2 && _ship.Energy < PlayerShip.EnergyMax / 2)
        {
            condition = 3;
        }

        return condition;
    }

    // Every piece of equipment the ship is carrying, in display order.
    private IEnumerable<string> EquipmentFitted()
    {
        if (_ship.CargoCapacity > 20)
        {
            yield return "Large Cargo Bay";
        }

        if (_ship.HasEscapeCapsule)
        {
            yield return "Escape Capsule";
        }

        if (_ship.HasFuelScoop)
        {
            yield return "Fuel Scoops";
        }

        if (_ship.HasECM)
        {
            yield return "E.C.M. System";
        }

        if (_ship.HasEnergyBomb)
        {
            yield return "Energy Bomb";
        }

        if (_ship.EnergyUnit != EnergyUnit.None)
        {
            yield return _ship.EnergyUnit == EnergyUnit.Extra ? "Extra Energy Unit" : "Naval Energy Unit";
        }

        if (_ship.HasDockingComputer)
        {
            yield return "Docking Computers";
        }

        if (_ship.HasGalacticHyperdrive)
        {
            yield return "Galactic Hyperspace";
        }

        if (_ship.LaserFront.Type != LaserType.None)
        {
            yield return $"Front {_ship.LaserFront.Name} Laser";
        }

        if (_ship.LaserRear.Type != LaserType.None)
        {
            yield return $"Rear {_ship.LaserRear.Name} Laser";
        }

        if (_ship.LaserLeft.Type != LaserType.None)
        {
            yield return $"Left {_ship.LaserLeft.Name} Laser";
        }

        if (_ship.LaserRight.Type != LaserType.None)
        {
            yield return $"Right {_ship.LaserRight.Name} Laser";
        }
    }
}
