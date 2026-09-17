// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Missions;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Conflict;
using EliteSharpLib.Missions;
using EliteSharpLib.Ships;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpKind.Input;
using SharpKind.Maths;

namespace EliteSharpLib.Views;

/// <summary>
/// The one screen every mission speaks on. On docking it asks each mission in
/// turn whether it wants to move; the first that comes back with something to
/// say gets the screen, and pressing space asks the ones after it, so a
/// commander who has earned two messages at once still sees both. When nobody
/// has anything the screen is left at once, which is what happens on almost
/// every docking.
/// <para>
/// Nothing here knows what any mission is about. It applies the step the
/// mission handed back, hands the briefing to the view, and does the two things
/// a briefing can ask of the world: put the ship it names on the stage, or
/// clear the stage when it names none.
/// </para>
/// </summary>
internal sealed class MissionBriefingController : IScreenController
{
    private readonly Combat _combat;
    private readonly GameState _gameState;
    private readonly IKeyboard _keyboard;
    private readonly MissionRunner _missions;
    private readonly PlayerShip _ship;
    private readonly Universe _universe;
    private readonly IShipFactory _shipFactory;
    private readonly ILogger<MissionBriefingController> _logger;
    private readonly IMissionBriefingView _view;

    /// <summary>
    /// The mission whose message is on screen, so that space asks the ones
    /// after it rather than starting again at the top.
    /// </summary>
    private string? _speaking;

    internal MissionBriefingController(
        GameState gameState,
        IKeyboard keyboard,
        PlayerShip ship,
        MissionRunner missions,
        Combat combat,
        Universe universe,
        IShipFactory shipFactory,
        IMissionBriefingView view,
        ILogger<MissionBriefingController>? logger = null)
    {
        _gameState = gameState;
        _keyboard = keyboard;
        _ship = ship;
        _missions = missions;
        _combat = combat;
        _universe = universe;
        _shipFactory = shipFactory;
        _view = view;
        _logger = logger ?? NullLogger<MissionBriefingController>.Instance;
    }

    // Exposed for tests: the whole of what the view is given.
    internal MissionBriefingModel Briefing { get; private set; } = MissionBriefingModel.Nothing;

    public void Draw() => _view.Draw(Briefing);

    public void HandleInput()
    {
        if (_keyboard.IsPressed(ConsoleKey.Spacebar))
        {
            ShowNext(after: _speaking);
        }
    }

    public void Reset() => ShowNext(after: null);

    public void Update()
    {
    }

    /// <summary>
    /// Gives the screen to the first mission after <paramref name="after"/>
    /// with something to say, or leaves the screen when none has.
    /// </summary>
    /// <param name="after">The mission already shown, or null to ask them all.</param>
    private void ShowNext(string? after)
    {
        bool reached = after is null;

        foreach (IMission mission in _missions.All)
        {
            if (!reached)
            {
                reached = string.Equals(mission.Name, after, StringComparison.Ordinal);
                continue;
            }

            if (_missions.Advance(mission) is not { } step)
            {
                continue;
            }

            _missions.Apply(mission, step);

            // A move with nothing to say is not a screen.
            if (step.Briefing is not { } briefing)
            {
                continue;
            }

            _speaking = mission.Name;
            Briefing = new(briefing.Headline ?? string.Empty, briefing.Paragraphs, briefing.Portrait is not null);
            SetTheStage(briefing.ShipName);

            return;
        }

        Briefing = MissionBriefingModel.Nothing;
        _speaking = null;
        _gameState.SetView(Screen.CommanderStatus);
    }

    /// <summary>
    /// Puts the briefing's ship where this tier shows it, or empties the stage
    /// when the briefing names none. One screen draws every briefing now, so it
    /// is no longer the screen that keeps the universe out of the picture:
    /// emptying it is.
    /// </summary>
    /// <param name="shipName">The ship the briefing names, or null for none.</param>
    private void SetTheStage(string? shipName)
    {
        _combat.Reset();
        _universe.ClearUniverse();

        if (shipName is null)
        {
            return;
        }

        IShip ship = _shipFactory.CreateShip(shipName);
        if (!_universe.AddNewShip(ship, _view.ShipLocation, VectorMaths.GetLeftHandedBasisMatrix, -127, -127))
        {
            LogMessages.FailedToCreateShip(_logger, shipName);
        }

        ship.Flags = ShipProperties.None;
        _ship.Roll = 0;
        _ship.Pitch = 0;
        _ship.Speed = 0;
    }
}
