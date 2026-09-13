// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Diagnostics;
using System.Numerics;
using EliteSharp.Abstractions.Ships;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Conflict;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;
using SharpKind.Graphics.Rendering;
using SharpKind.Input;

namespace EliteSharpLib.Views;

/// <summary>
/// A cockpit window's behaviour: flight, docking and weapon controls, which
/// are identical looking front, rear, left or right, so one controller
/// serves all four - its <see cref="PilotDirection"/> only selects the view
/// name, the laser mount and the starfield to scroll.
/// </summary>
internal sealed class PilotController : IScreenController
{
    /// <summary>
    /// How long a laser bolt stays on screen once fired, in the game's ticks.
    /// </summary>
    private const float LaserVisibleTicks = 2;

    private readonly GameState _gameState;
    private readonly IKeyboard _keyboard;
    private readonly IGamepad _gamepad;
    private readonly Pilot _pilot;
    private readonly PlayerShip _ship;
    private readonly Stars _stars;
    private readonly Space _space;
    private readonly Combat _combat;
    private readonly PilotDirection _direction;
    private readonly IEliteDraw _draw;
    private readonly IView<PilotModel> _view;

    // How much longer the laser bolt is drawn for, in the game's own ticks
    // rather than in updates - or the beam would be a flicker a third as
    // long at sixty frames a second as at thirteen and a half.
    private float _drawLaserTicks;

    internal PilotController(
        GameState gameState,
        IKeyboard keyboard,
        IGamepad gamepad,
        Pilot pilot,
        PlayerShip ship,
        Stars stars,
        Space space,
        Combat combat,
        PilotDirection direction,
        IEliteDraw draw,
        IView<PilotModel> view)
    {
        _gameState = gameState;
        _keyboard = keyboard;
        _gamepad = gamepad;
        _pilot = pilot;
        _ship = ship;
        _stars = stars;
        _space = space;
        _combat = combat;
        _draw = draw;
        _direction = direction;
        _view = view;
    }

    public void Draw() => _view.Draw(BuildModel());

    // Continuous flight controls (pitch/roll/speed/fire) are polled every
    // frame and need IsHeld's non-consuming "is the key currently down"
    // state, not IsPressed's one-shot consumption - otherwise a held key
    // would go unresponsive as soon as a second key was also held (SDL/
    // Windows key-repeat only re-fires for the most recently pressed key).
    // One-shot commands below (docking, hyperspace, missiles, pause, etc.)
    // correctly keep using IsPressed.
    public void HandleInput()
    {
        HandleFlightControls();
        HandleNavigationCommands();
        HandleWeaponCommands();
    }

    public void Reset() => _stars.FlipStars();

    public void Update()
    {
        _drawLaserTicks = _gameState.DrawLasers
            ? LaserVisibleTicks
            : MathF.Max(_drawLaserTicks - _gameState.Clock.Ticks, 0);

        switch (_direction)
        {
            case PilotDirection.Front:
                _stars.FrontStarfield();
                break;

            case PilotDirection.Rear:
                _stars.RearStarfield();
                break;

            case PilotDirection.Left:
                _stars.LeftStarfield();
                break;

            case PilotDirection.Right:
                _stars.RightStarfield();
                break;
        }
    }

    // Exposed for tests: the view name, the hyperspace status text and
    // this direction's laser state.
    internal PilotModel BuildModel()
    {
        string hyperspaceStatus = _space.HyperGalactic
            ? "Galactic Hyperspace"
            : _space.HyperCountdown > 0 ? $"Hyperspace - {_space.HyperName}" : string.Empty;

        (string viewName, LaserType laserType) = _direction switch
        {
            PilotDirection.Front => ("Front View", _ship.LaserFront.Type),
            PilotDirection.Rear => ("Rear View", _ship.LaserRear.Type),
            PilotDirection.Left => ("Left View", _ship.LaserLeft.Type),
            PilotDirection.Right => ("Right View", _ship.LaserRight.Type),
            _ => throw new UnreachableException(),
        };

        // The beams meet a pixel or two off centre, rolled fresh every frame -
        // that shimmer is the original's. The roll happens here because the
        // game owns the one source of entropy; a view that rolled its own
        // would not be reproducible.
        Vector2 laserAim = new(_draw.Jitter.Random(0, 2), _draw.Jitter.Random(0, 2));

        return new(
            viewName,
            hyperspaceStatus,
            laserType,
            _drawLaserTicks > 0,
            laserAim,
            _gameState.Config.Engine.Graphics.FillMode == FillMode.Wireframe);
    }

    // Each flight control answers to a key or the pad, so the handlers below
    // stay one branch per control rather than one branch per input device.
    private bool WantsFire()
        => _keyboard.IsHeld(ConsoleKey.A)
            || GamepadControls.IsFiring(_gamepad);

    private bool WantsPitchUp()
        => _keyboard.IsHeld(ConsoleKey.S)
            || _keyboard.IsHeld(ConsoleKey.UpArrow)
            || GamepadControls.Pitch(_gamepad) < 0;

    private bool WantsPitchDown()
        => _keyboard.IsHeld(ConsoleKey.X)
            || _keyboard.IsHeld(ConsoleKey.DownArrow)
            || GamepadControls.Pitch(_gamepad) > 0;

    private bool WantsRollLeft()
        => _keyboard.IsHeld(ConsoleKey.OemComma)
            || _keyboard.IsHeld(ConsoleKey.LeftArrow)
            || GamepadControls.Roll(_gamepad) < 0;

    private bool WantsRollRight()
        => _keyboard.IsHeld(ConsoleKey.OemPeriod)
            || _keyboard.IsHeld(ConsoleKey.RightArrow)
            || GamepadControls.Roll(_gamepad) > 0;

    private bool WantsAccelerate()
        => _keyboard.IsHeld(ConsoleKey.Spacebar)
            || GamepadControls.IsAccelerating(_gamepad);

    private bool WantsDecelerate()
        => _keyboard.IsHeld(ConsoleKey.Oem2)
            || GamepadControls.IsDecelerating(_gamepad);

    // Yaw left and right. Q and W, because comma and full stop are already
    // the roll, and the stick's twist. Off unless the commander switched it
    // on, so the check is here rather than in the ship: with yaw off nothing
    // reads these controls at all.
    private bool WantsYawLeft()
        => DebugYaw.IsEnabled
            && (_keyboard.IsHeld(ConsoleKey.Q)
                || GamepadControls.Yaw(_gamepad) < 0);

    private bool WantsYawRight()
        => DebugYaw.IsEnabled
            && (_keyboard.IsHeld(ConsoleKey.W)
                || GamepadControls.Yaw(_gamepad) > 0);

    private void HandleFlightControls()
    {
        if (WantsFire())
        {
            _gameState.DrawLasers = _combat.FireLaser();
        }

        HandlePitchControls();
        HandleRollControls();
        HandleYawControls();

        if (WantsAccelerate() && !_gameState.IsDocked)
        {
            _ship.IncreaseSpeed();
        }

        if (WantsDecelerate() && !_gameState.IsDocked)
        {
            _ship.DecreaseSpeed();
        }
    }

    // An analog stick is not a key held down, so it does not go through the
    // ramp: where it is pushed to *is* the rate of turn, reached at once and
    // dropped at once when the stick comes back. That is how a flight stick
    // behaves, and it is only possible for a device that can report a
    // position - a digital stick has none to give, so it keeps the ramp,
    // which is the whole of its feel.
    //
    // The control is taken on the strength of the axis being analog, not of
    // it being pushed: a centred stick is commanding a rate of zero, and it
    // has to be able to say so. Leaving it to the deflection instead put the
    // centre back in the hands of LevelOut, which is the damping this is
    // here to get rid of - the ship would snap into the turn and then sag
    // out of it over two seconds.
    //
    // The keyboard still wins where it is being used, so a stick plugged in
    // and left alone cannot pin a control at zero and lock the keys out.
    // Returns whether the axis took the control, in which case the caller
    // leaves the key path alone rather than applying the stick twice.
    private bool ApplyAxis(GamepadAxis axis, float maxRate, bool keysActive, Action<float> setRate)
    {
        if (keysActive || !_gamepad.IsAnalog(axis))
        {
            return false;
        }

        setRate(GamepadControls.Deflection(_gamepad, axis) * maxRate);
        return true;
    }

    // The keyboard halves of the flight controls, apart from the stick's, so
    // an analog axis can tell whether the pilot is using the keys instead.
    private bool RollKeysHeld()
        => _keyboard.IsHeld(ConsoleKey.OemComma)
            || _keyboard.IsHeld(ConsoleKey.LeftArrow)
            || _keyboard.IsHeld(ConsoleKey.OemPeriod)
            || _keyboard.IsHeld(ConsoleKey.RightArrow);

    private bool PitchKeysHeld()
        => _keyboard.IsHeld(ConsoleKey.S)
            || _keyboard.IsHeld(ConsoleKey.UpArrow)
            || _keyboard.IsHeld(ConsoleKey.X)
            || _keyboard.IsHeld(ConsoleKey.DownArrow);

    private bool YawKeysHeld()
        => _keyboard.IsHeld(ConsoleKey.Q) || _keyboard.IsHeld(ConsoleKey.W);

    // Pitch up and down, the stick's own sense: SDL's Y is positive
    // downwards and so is the ship's pitch, so the deflection carries
    // straight across without a sign flip.
    private void HandlePitchControls()
    {
        if (ApplyAxis(GamepadAxis.LeftY, _ship.MaxPitch, PitchKeysHeld(), rate => _ship.Pitch = rate))
        {
            _ship.IsPitching = true;
            return;
        }

        if (WantsPitchUp())
        {
            if (_ship.Pitch > 0)
            {
                _ship.Pitch = 0;
            }
            else
            {
                _ship.DecreasePitch();
                _ship.DecreasePitch();
            }

            _ship.IsPitching = true;
        }

        if (WantsPitchDown())
        {
            if (_ship.Pitch < 0)
            {
                _ship.Pitch = 0;
            }
            else
            {
                _ship.IncreasePitch();
                _ship.IncreasePitch();
            }

            _ship.IsPitching = true;
        }
    }

    // Roll left and right. A roll in the opposite direction to the current one
    // levels the ship out instead.
    private void HandleRollControls()
    {
        // Negated: the stick's X is positive to the right, where a roll to
        // the right is a negative rate.
        if (ApplyAxis(GamepadAxis.LeftX, -_ship.MaxRoll, RollKeysHeld(), rate => _ship.Roll = rate))
        {
            _ship.IsRolling = true;
            return;
        }

        if (WantsRollLeft())
        {
            if (_ship.Roll < 0)
            {
                _ship.Roll = 0;
            }
            else
            {
                _ship.IncreaseRoll();
                _ship.IncreaseRoll();
                _ship.IsRolling = true;
            }
        }

        if (WantsRollRight())
        {
            if (_ship.Roll > 0)
            {
                _ship.Roll = 0;
            }
            else
            {
                _ship.DecreaseRoll();
                _ship.DecreaseRoll();
                _ship.IsRolling = true;
            }
        }
    }

    // Yaw levels out against itself the way the roll does: a yaw the other
    // way stops the turn rather than reversing it.
    private void HandleYawControls()
    {
        if (DebugYaw.IsEnabled
            && ApplyAxis(GamepadAxis.RightX, _ship.MaxYaw, YawKeysHeld(), rate => _ship.Yaw = rate))
        {
            _ship.IsYawing = true;
            return;
        }

        if (WantsYawLeft())
        {
            if (_ship.Yaw > 0)
            {
                _ship.Yaw = 0;
            }
            else
            {
                _ship.DecreaseYaw();
                _ship.DecreaseYaw();
                _ship.IsYawing = true;
            }
        }

        if (WantsYawRight())
        {
            if (_ship.Yaw < 0)
            {
                _ship.Yaw = 0;
            }
            else
            {
                _ship.IncreaseYaw();
                _ship.IncreaseYaw();
                _ship.IsYawing = true;
            }
        }
    }

    private void HandleNavigationCommands()
    {
        if (_keyboard.IsPressed(ConsoleKey.C) &&
            !_gameState.IsDocked
            && _ship.HasDockingComputer)
        {
            EngageDockingComputer();
        }

        if (_keyboard.IsPressed(ConsoleKey.D))
        {
            _pilot.DisengageAutoPilot();
        }

        if (_keyboard.IsPressed(ConsoleKey.H) && (!_gameState.IsDocked))
        {
            // Held, not pressed: Ctrl only picks which hyperspace this is, and
            // consuming it would take it from any other Ctrl combination read
            // later in the same tick.
            if (_keyboard.IsHeld(ConsoleModifiers.Control))
            {
                _space.StartGalacticHyperspace();
            }
            else
            {
                _space.StartHyperspace();
            }
        }

        if (_keyboard.IsPressed(ConsoleKey.J) &&
            (!_gameState.IsDocked)
            && (!_gameState.InWitchspace))
        {
            _space.JumpWarp();
        }

        if (_keyboard.IsPressed(ConsoleKey.P))
        {
            _gameState.IsGamePaused = true;
        }

        if (_keyboard.IsPressed(ConsoleKey.Escape) &&
            (!_gameState.IsDocked)
            && _ship.HasEscapeCapsule
            && (!_gameState.InWitchspace))
        {
            _gameState.SetView(Screen.EscapeCapsule);
        }
    }

    // Dock instantly if configured to, otherwise fly the ship in on autopilot.
    private void EngageDockingComputer()
    {
        if (_gameState.Config.Game.InstantDock)
        {
            _space.EngageDockingComputer();
        }
        else if (!_gameState.InWitchspace && !_space.IsHyperspaceReady)
        {
            _pilot.EngageAutoPilot();
        }
    }

    private void HandleWeaponCommands()
    {
        if (_keyboard.IsPressed(ConsoleKey.E) &&
            !_gameState.IsDocked
            && _ship.HasECM)
        {
            _combat.ActivateECM(true);
        }

        if (_keyboard.IsPressed(ConsoleKey.M) &&
            !_gameState.IsDocked)
        {
            _combat.FireMissile();
        }

        if (_keyboard.IsPressed(ConsoleKey.T) &&
            !_gameState.IsDocked)
        {
            _combat.ArmMissile();
        }

        if (_keyboard.IsPressed(ConsoleKey.U) &&
            !_gameState.IsDocked)
        {
            _combat.UnarmMissile();
        }

        if (_keyboard.IsPressed(ConsoleKey.Tab) &&
            (!_gameState.IsDocked)
            && _ship.HasEnergyBomb)
        {
            _gameState.DetonateBomb = true;
            _ship.HasEnergyBomb = false;
        }

        if (_keyboard.IsPressed(ConsoleKey.Escape) &&
            (!_gameState.IsDocked)
            && _ship.HasEscapeCapsule
            && (!_gameState.InWitchspace))
        {
            _gameState.SetView(Screen.EscapeCapsule);
        }
    }
}
