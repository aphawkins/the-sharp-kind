// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Audio;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;
using EliteSharpLib.Trader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpKind.Audio;
using SharpKind.Maths;

namespace EliteSharpLib.Views;

/// <summary>
/// The escape capsule sequence's behaviour: the abandoned Cobra blowing up
/// behind the capsule, then the automatic docking that follows.
/// </summary>
internal sealed class EscapeCapsuleController : IScreenController
{
    private const string Alert = "Escape capsule launched - Ship auto-destuct initiated.";

    // Ticks the capsule spends watching the ship go; the explosion is at tick 40.
    private const float LaunchTicks = 90;
    private const float ExplosionTick = 40;

    private readonly AudioController _audio;
    private readonly GameState _gameState;
    private readonly Pilot _pilot;
    private readonly PlayerShip _ship;
    private readonly IShipFactory _shipFactory;
    private readonly Stars _stars;
    private readonly Trade _trade;
    private readonly Universe _universe;
    private readonly ILogger<EscapeCapsuleController> _logger;
    private readonly IView<EscapeCapsuleModel> _view;
    private float _tick;
    private IShip _newShip;

    internal EscapeCapsuleController(
        GameState gameState,
        AudioController audio,
        Stars stars,
        PlayerShip ship,
        Trade trade,
        Universe universe,
        Pilot pilot,
        IEliteDraw draw,
        IShipFactory shipFactory,
        IView<EscapeCapsuleModel> view,
        ILogger<EscapeCapsuleController>? logger = null)
    {
        _gameState = gameState;
        _audio = audio;
        _stars = stars;
        _ship = ship;
        _trade = trade;
        _universe = universe;
        _pilot = pilot;
        _shipFactory = shipFactory;
        _newShip = new ShipBase(draw);
        _view = view;
        _logger = logger ?? NullLogger<EscapeCapsuleController>.Instance;
    }

    public void Draw() => _view.Draw(BuildModel());

    public void HandleInput()
    {
    }

    public void Reset()
    {
        _ship.Speed = 1;
        _ship.Roll = 0;
        _ship.Pitch = 0;
        Matrix4x4 rotmat = VectorMaths.GetRightHandedBasisMatrix;
        _newShip = _shipFactory.CreateShip("CobraMk3");
        if (!_universe.AddNewShip(_newShip, new(0, 0, 200, 0), rotmat, -127, -127))
        {
            LogMessages.FailedToCreateShip(_logger, "CobraMk3");
        }

        _newShip.Velocity = 7;
        _audio.PlayEffect(nameof(SoundEffect.Launch));
        _tick = 0;
    }

    public void Update()
    {
        if (_tick < LaunchTicks)
        {
            float ticks = _gameState.Clock.Ticks;

            BlowUpTheAbandonedShip(ticks);
            _stars.FrontStarfield();
            _newShip.Location = new(0, 0, _newShip.Location.Z + (2 * ticks), 0);
            _tick += ticks;
        }
        else if (!_universe.IsStationPresent)
        {
            _pilot.AutoDock();

            if ((MathF.Abs(_ship.Roll) < 3) && (MathF.Abs(_ship.Pitch) < 3))
            {
                foreach (IObject universeObj in _universe.GetAllObjects())
                {
                    if (universeObj.Id != ObjectIds.None)
                    {
                        universeObj.Location = new(universeObj.Location.X, universeObj.Location.Y, universeObj.Location.Z - 1500, 0);
                    }
                }
            }

            _stars.WarpStars = true;
            _stars.FrontStarfield();
        }
        else
        {
            _ship.HasEscapeCapsule = false;
            _gameState.Cmdr.LegalStatus = 0;
            _ship.Fuel = _ship.MaxFuel;
            _trade.ClearCurrentCargo();
            _gameState.SetView(Screen.Docking);
        }
    }

    // Exposed for tests: the alert is only up while the ship is still going.
    internal EscapeCapsuleModel BuildModel() => new(Alert, _tick < LaunchTicks);

    // Crossed, not equalled: a fractional tick can step past the explosion moment and miss an == test.
    private void BlowUpTheAbandonedShip(float ticks)
    {
        if (_tick < ExplosionTick && _tick + ticks >= ExplosionTick)
        {
            _newShip.Flags |= ShipProperties.Dead;
            _audio.PlayEffect(nameof(SoundEffect.Explode));
        }
    }
}
