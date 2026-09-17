// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharpLib.Audio;
using EliteSharpLib.Conflict;
using EliteSharpLib.Ships;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpKind.Audio;
using SharpKind.Maths;

namespace EliteSharpLib.Views;

/// <summary>
/// The game over screen's behaviour: the dead Cobra and its scattered cargo
/// put into the universe, and the hundred-tick wait before the game resets.
/// </summary>
internal sealed class GameOverController : IScreenController
{
    // How many ticks the wreckage tumbles before the game restarts.
    private const float TicksBeforeRestart = 100;

    private readonly AudioController _audio;
    private readonly Combat _combat;
    private readonly GameState _gameState;
    private readonly PlayerShip _ship;
    private readonly Stars _stars;
    private readonly Universe _universe;
    private readonly IShipFactory _shipFactory;
    private readonly ILogger<GameOverController> _logger;
    private readonly RNG _rng;
    private readonly IView<GameOverModel> _view;

    private float _tick;

    internal GameOverController(
        GameState gameState,
        AudioController audio,
        Stars stars,
        PlayerShip ship,
        Combat combat,
        Universe universe,
        IShipFactory shipFactory,
        RNG rng,
        IView<GameOverModel> view,
        ILogger<GameOverController>? logger = null)
    {
        _gameState = gameState;
        _audio = audio;
        _stars = stars;
        _ship = ship;
        _combat = combat;
        _universe = universe;
        _shipFactory = shipFactory;
        _rng = rng;
        _view = view;
        _logger = logger ?? NullLogger<GameOverController>.Instance;
    }

    // The wording never changes, so the model is built once.
    private static GameOverModel Model { get; } = new("GAME OVER");

    public void Draw() => _view.Draw(Model);

    public void HandleInput()
    {
    }

    public void Reset()
    {
        _tick = 0;
        _ship.Speed = 6;
        _ship.Roll = 0;
        _ship.Pitch = 0;
        _combat.Reset();
        _universe.ClearUniverse();
        IShip cobraMk3 = _shipFactory.CreateShip("CobraMk3");
        if (!_universe.AddNewShip(cobraMk3, new(0, 0, -400, 0), VectorMaths.GetLeftHandedBasisMatrix, 0, 0))
        {
            LogMessages.FailedToCreateShip(_logger, "CobraMk3");
        }

        cobraMk3.Flags |= ShipProperties.Dead;

        for (int i = 0; i < 5; i++)
        {
            IShip cargo = _rng.TrueOrFalse() ? _shipFactory.CreateShip("CargoCannister") : _shipFactory.CreateShip("Alloy");
            if (!_universe.AddNewShip(
                cargo,
                new(_rng.Random(-32, 32), _rng.Random(-32, 32), -400, 0),
                VectorMaths.GetLeftHandedBasisMatrix,
                0,
                0))
            {
                LogMessages.FailedToCreateShip(_logger, "Cargo");
            }

            cargo.RotZ = ((_rng.Random(256) * 2) & 255) - 128;
            cargo.RotX = ((_rng.Random(256) * 2) & 255) - 128;
            cargo.Velocity = _rng.Random(16);
        }

        _audio.PlayEffect(nameof(SoundEffect.Gameover));
    }

    public void Update()
    {
        if (_tick >= TicksBeforeRestart)
        {
            _gameState.IsInitialised = false;
        }

        _stars.RearStarfield();
        _tick += _gameState.Clock.Ticks;
    }
}
