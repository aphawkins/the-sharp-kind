// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Ships;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Equipment;
using EliteSharpLib.Lasers;
using EliteSharpLib.Ships;
using EliteSharpLib.Trader;
using EliteSharpLib.Types;
using SharpKind.Input;

namespace EliteSharpLib.Views;

/// <summary>
/// The equip-ship screen's behaviour: the cursor over the stock list,
/// expanding a laser category into its mount choices, and buying whatever
/// the cursor is on.
/// </summary>
internal sealed class EquipmentController : IScreenController
{
    private readonly EquipmentItem[] _equipmentStock =
    [
        new(false, true, 1, 0.2f, " Fuel", EquipmentType.Fuel),
        new(false, true, 1, 30, " Missile", EquipmentType.Missile),
        new(false, true, 1, 400, " Large Cargo Bay", EquipmentType.CargoBay),
        new(false, true, 2, 600, " E.C.M. System", EquipmentType.ECM),
        new(false, true, 5, 525, " Fuel Scoops", EquipmentType.FuelScoop),
        new(false, true, 6, 1000, " Escape Capsule", EquipmentType.EscapeCapsule),
        new(false, true, 7, 900, " Energy Bomb", EquipmentType.EnergyBomb),
        new(false, true, 8, 1500, " Extra Energy Unit", EquipmentType.EnergyUnit),
        new(false, true, 9, 1000, " Docking Computers", EquipmentType.DockingComputer),
        new(false, true, 10, 5000, " Galactic Hyperdrive", EquipmentType.GalacticHyperdrive),
        new(false, false, 3, 400, "+Pulse Laser", EquipmentType.PulseLaser),
        new(false, true, 3, 0, "-Pulse Laser", EquipmentType.PulseLaser),
        new(false, true, 3, 400, ">Front", EquipmentType.PulseFront),
        new(false, true, 3, 400, ">Rear", EquipmentType.PulseRear),
        new(false, true, 3, 400, ">Left", EquipmentType.PulseLeft),
        new(false, true, 3, 400, ">Right", EquipmentType.PulseRight),
        new(false, true, 4, 1000, "+Beam Laser", EquipmentType.BeamLaser),
        new(false, false, 4, 0, "-Beam Laser", EquipmentType.BeamLaser),
        new(false, false, 4, 1000, ">Front", EquipmentType.BeamFront),
        new(false, false, 4, 1000, ">Rear", EquipmentType.BeamRear),
        new(false, false, 4, 1000, ">Left", EquipmentType.BeamLeft),
        new(false, false, 4, 1000, ">Right", EquipmentType.BeamRight),
        new(false, true, 12, 800, "+Mining Laser", EquipmentType.MiningLaser),
        new(false, false, 12, 0, "-Mining Laser", EquipmentType.MiningLaser),
        new(false, false, 12, 800, ">Front", EquipmentType.MiningFront),
        new(false, false, 12, 800, ">Rear", EquipmentType.MiningRear),
        new(false, false, 12, 800, ">Left", EquipmentType.MiningLeft),
        new(false, false, 12, 800, ">Right", EquipmentType.MiningRight),
        new(false, true, 11, 6000, "+Military Laser", EquipmentType.MilitaryLaser),
        new(false, false, 11, 0, "-Military Laser", EquipmentType.MilitaryLaser),
        new(false, false, 11, 6000, ">Front", EquipmentType.MilitaryFront),
        new(false, false, 11, 6000, ">Rear", EquipmentType.MilitaryRear),
        new(false, false, 11, 6000, ">Left", EquipmentType.MilitaryLeft),
        new(false, false, 11, 6000, ">Right", EquipmentType.MilitaryRight),
    ];

    private readonly GameState _gameState;
    private readonly IKeyboard _keyboard;
    private readonly ScannerController _scanner;
    private readonly PlayerShip _ship;
    private readonly Trade _trade;
    private readonly IView<EquipmentModel> _view;

    private int _highlightedItem;

    internal EquipmentController(
        GameState gameState,
        IKeyboard keyboard,
        PlayerShip ship,
        Trade trade,
        ScannerController scanner,
        IView<EquipmentModel> view)
    {
        _gameState = gameState;
        _keyboard = keyboard;
        _ship = ship;
        _trade = trade;
        _scanner = scanner;
        _view = view;
    }

    public void Draw() => _view.Draw(BuildModel());

    public void HandleInput()
    {
        if (_keyboard.IsPressed(ConsoleKey.S) || _keyboard.IsPressed(ConsoleKey.UpArrow))
        {
            SelectPrevious();
        }

        if (_keyboard.IsPressed(ConsoleKey.X) || _keyboard.IsPressed(ConsoleKey.DownArrow))
        {
            SelectNext();
        }

        if (_keyboard.IsPressed(ConsoleKey.Enter))
        {
            Buy();
        }
    }

    public void Reset()
    {
        CollapseList();

        _highlightedItem = 0;

        ListPrices();
    }

    public void Update()
    {
    }

    // Exposed for tests: the visible rows in draw order, and which one the cursor is on.
    internal EquipmentModel BuildModel()
    {
        List<EquipmentRow> rows = [];
        foreach (EquipmentItem item in _equipmentStock)
        {
            if (!item.Show)
            {
                continue;
            }

            rows.Add(new(
                item.Name[1..],
                item.Name[0] == '>',
                item.CanBuy,
                _equipmentStock[_highlightedItem] == item,
                (int)item.Price != 0 ? $"{item.Price:N1}" : string.Empty));
        }

        return new(rows, $"Cash: {_trade.Credits:N1} Credits");
    }

    internal void Buy()
    {
        if (_equipmentStock[_highlightedItem].Name[0] == '+')
        {
            CollapseList();
            _equipmentStock[_highlightedItem].Show = false;
            _highlightedItem++;
            for (int i = 0; i < 5; i++)
            {
                _equipmentStock[_highlightedItem + i].Show = true;
            }

            ListPrices();
            return;
        }

        if (!_equipmentStock[_highlightedItem].CanBuy)
        {
            return;
        }

        switch (_equipmentStock[_highlightedItem].Type)
        {
            case EquipmentType.Fuel:
                _ship.Fuel = _ship.MaxFuel;
                _scanner.UpdateConsole();
                break;

            case EquipmentType.Missile:
                _ship.MissileCount++;
                _scanner.UpdateConsole();
                break;

            case EquipmentType.CargoBay:
                _ship.CargoCapacity = 35;
                break;

            case EquipmentType.ECM:
                _ship.HasECM = true;
                break;

            case EquipmentType.FuelScoop:
                _ship.HasFuelScoop = true;
                break;

            case EquipmentType.EscapeCapsule:
                _ship.HasEscapeCapsule = true;
                break;

            case EquipmentType.EnergyBomb:
                _ship.HasEnergyBomb = true;
                break;

            case EquipmentType.EnergyUnit:
                _ship.EnergyUnit = EnergyUnit.Extra;
                break;

            case EquipmentType.DockingComputer:
                _ship.HasDockingComputer = true;
                break;

            case EquipmentType.GalacticHyperdrive:
                _ship.HasGalacticHyperdrive = true;
                break;

            default:
                BuyLaser(_equipmentStock[_highlightedItem].Type);
                break;
        }

        _trade.Credits -= _equipmentStock[_highlightedItem].Price;
        ListPrices();
    }

    internal void SelectNext()
    {
        if (_highlightedItem == _equipmentStock.Length - 1)
        {
            return;
        }

        for (int i = _highlightedItem + 1; i < _equipmentStock.Length; i++)
        {
            if (_equipmentStock[i].Show)
            {
                _highlightedItem = i;
                break;
            }
        }
    }

    internal void SelectPrevious()
    {
        if (_highlightedItem == 0)
        {
            return;
        }

        for (int i = _highlightedItem - 1; i >= 0; i--)
        {
            if (_equipmentStock[i].Show)
            {
                _highlightedItem = i;
                break;
            }
        }
    }

    private static float LaserRefund(LaserType laserType) => laserType switch
    {
        LaserType.Pulse => 400,
        LaserType.Beam => 1000,
        LaserType.Military => 6000,
        LaserType.Mining => 800,
        LaserType.None => 0,
        _ => 0,
    };

    private void BuyLaser(EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.PulseFront:
                _trade.Credits += LaserRefund(_ship.LaserFront.Type);
                _ship.LaserFront = new PulseLaser();
                break;

            case EquipmentType.PulseRear:
                _trade.Credits += LaserRefund(_ship.LaserRear.Type);
                _ship.LaserRear = new PulseLaser();
                break;

            case EquipmentType.PulseLeft:
                _trade.Credits += LaserRefund(_ship.LaserLeft.Type);
                _ship.LaserLeft = new PulseLaser();
                break;

            case EquipmentType.PulseRight:
                _trade.Credits += LaserRefund(_ship.LaserRight.Type);
                _ship.LaserRight = new PulseLaser();
                break;

            case EquipmentType.BeamFront:
                _trade.Credits += LaserRefund(_ship.LaserFront.Type);
                _ship.LaserFront = new BeamLaser();
                break;

            case EquipmentType.BeamRear:
                _trade.Credits += LaserRefund(_ship.LaserRear.Type);
                _ship.LaserRear = new BeamLaser();
                break;

            case EquipmentType.BeamLeft:
                _trade.Credits += LaserRefund(_ship.LaserLeft.Type);
                _ship.LaserLeft = new BeamLaser();
                break;

            case EquipmentType.BeamRight:
                _trade.Credits += LaserRefund(_ship.LaserRight.Type);
                _ship.LaserRight = new BeamLaser();
                break;

            case EquipmentType.MiningFront:
                _trade.Credits += LaserRefund(_ship.LaserFront.Type);
                _ship.LaserFront = new MiningLaser();
                break;

            case EquipmentType.MiningRear:
                _trade.Credits += LaserRefund(_ship.LaserRear.Type);
                _ship.LaserRear = new MiningLaser();
                break;

            case EquipmentType.MiningLeft:
                _trade.Credits += LaserRefund(_ship.LaserLeft.Type);
                _ship.LaserLeft = new MiningLaser();
                break;

            case EquipmentType.MiningRight:
                _trade.Credits += LaserRefund(_ship.LaserRight.Type);
                _ship.LaserRight = new MiningLaser();
                break;

            case EquipmentType.MilitaryFront:
                _trade.Credits += LaserRefund(_ship.LaserFront.Type);
                _ship.LaserFront = new MilitaryLaser();
                break;

            case EquipmentType.MilitaryRear:
                _trade.Credits += LaserRefund(_ship.LaserRear.Type);
                _ship.LaserRear = new MilitaryLaser();
                break;

            case EquipmentType.MilitaryLeft:
                _trade.Credits += LaserRefund(_ship.LaserLeft.Type);
                _ship.LaserLeft = new MilitaryLaser();
                break;

            case EquipmentType.MilitaryRight:
                _trade.Credits += LaserRefund(_ship.LaserRight.Type);
                _ship.LaserRight = new MilitaryLaser();
                break;
        }
    }

    private void CollapseList()
    {
        for (int i = 0; i < _equipmentStock.Length; i++)
        {
            _equipmentStock[i].Show = _equipmentStock[i].Name[0] is ' ' or '+';
        }
    }

    private void ListPrices()
    {
        int techLevel = _gameState.CurrentPlanetData.TechLevel + 1;

        _equipmentStock[0].Price = (7 - _ship.Fuel) * 2;

        for (int i = 0; i < _equipmentStock.Length; i++)
        {
            _equipmentStock[i].CanBuy = !PresentEquipment(_equipmentStock[i].Type) && _equipmentStock[i].Price <= _trade.Credits;
            _equipmentStock[i].Show = _equipmentStock[i].Show && techLevel >= _equipmentStock[i].TechLevel;
        }

        _highlightedItem = 0;
    }

    private bool PresentEquipment(EquipmentType type) => type switch
    {
        EquipmentType.Fuel => _ship.Fuel >= 7,
        EquipmentType.Missile => _ship.MissileCount >= 4,
        EquipmentType.CargoBay => _ship.CargoCapacity > 20,
        EquipmentType.ECM => _ship.HasECM,
        EquipmentType.FuelScoop => _ship.HasFuelScoop,
        EquipmentType.EscapeCapsule => _ship.HasEscapeCapsule,
        EquipmentType.EnergyBomb => _ship.HasEnergyBomb,
        EquipmentType.EnergyUnit => _ship.EnergyUnit != EnergyUnit.None,
        EquipmentType.DockingComputer => _ship.HasDockingComputer,
        EquipmentType.GalacticHyperdrive => _ship.HasGalacticHyperdrive,
        EquipmentType.PulseFront => _ship.LaserFront.Type == LaserType.Pulse,
        EquipmentType.PulseRear => _ship.LaserRear.Type == LaserType.Pulse,
        EquipmentType.PulseLeft => _ship.LaserLeft.Type == LaserType.Pulse,
        EquipmentType.PulseRight => _ship.LaserRight.Type == LaserType.Pulse,
        EquipmentType.BeamFront => _ship.LaserFront.Type == LaserType.Beam,
        EquipmentType.BeamRear => _ship.LaserRear.Type == LaserType.Beam,
        EquipmentType.BeamLeft => _ship.LaserLeft.Type == LaserType.Beam,
        EquipmentType.BeamRight => _ship.LaserRight.Type == LaserType.Beam,
        EquipmentType.MiningFront => _ship.LaserFront.Type == LaserType.Mining,
        EquipmentType.MiningRear => _ship.LaserRear.Type == LaserType.Mining,
        EquipmentType.MiningLeft => _ship.LaserLeft.Type == LaserType.Mining,
        EquipmentType.MiningRight => _ship.LaserRight.Type == LaserType.Mining,
        EquipmentType.MilitaryFront => _ship.LaserFront.Type == LaserType.Military,
        EquipmentType.MilitaryRear => _ship.LaserRear.Type == LaserType.Military,
        EquipmentType.MilitaryLeft => _ship.LaserLeft.Type == LaserType.Military,
        EquipmentType.MilitaryRight => _ship.LaserRight.Type == LaserType.Military,
        EquipmentType.PulseLaser => false,
        EquipmentType.BeamLaser => false,
        EquipmentType.MiningLaser => false,
        EquipmentType.MilitaryLaser => false,
        EquipmentType.None => false,
        _ => false,
    };
}
