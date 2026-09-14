// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Diagnostics;
using System.Numerics;
using EliteSharp.Abstractions.Ships;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Conflict;
using EliteSharpLib.Controls;
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
    private readonly EliteControlMap _controls;

    // Only for the Ctrl that turns hyperspace galactic. A modifier is not
    // an action and has no place in the bindings file: it decorates another
    // control rather than being one.
    private readonly IKeyboard _keyboard;
    private readonly Pilot _pilot;
    private readonly PlayerShip _ship;
    private readonly Stars _stars;
    private readonly Space _space;
    private readonly Combat _combat;
    private readonly PilotDirection _direction;
    private readonly IEliteDraw _draw;
    private readonly IView<PilotModel> _view;

    // The stick's one-shot commands. The gamepad reports these as holds
    // rather than presses, because one profile puts fire-missile on a
    // trigger and a trigger is an axis with no press to consume, so the
    // edge is made here for all of them rather than half here and half in
    // the device.
    private readonly Edge _fireMissile = new();
    private readonly Edge _targetMissile = new();
    private readonly Edge _untargetMissile = new();
    private readonly Edge _ecm = new();
    private readonly Edge _warpJump = new();
    private readonly Edge _dockingComputer = new();
    private readonly Edge _hyperspace = new();

    // How much longer the laser bolt is drawn for, in the game's own ticks
    // rather than in updates - or the beam would be a flicker a third as
    // long at sixty frames a second as at thirteen and a half.
    private float _drawLaserTicks;

    internal PilotController(
        GameState gameState,
        EliteControlMap controls,
        IKeyboard keyboard,
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
        _controls = controls;
        _keyboard = keyboard;
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

    // Every flight control is one question now - is this action wanted -
    // and the map answers for the keyboard and whichever stick is being
    // flown alike. Yaw is off unless the commander switched it on, so the
    // check is here rather than in the ship: with yaw off nothing reads
    // these controls at all.
    private bool WantsYawLeft() => DebugYaw.IsEnabled && _controls.IsHeld(EliteAction.YawLeft);

    private bool WantsYawRight() => DebugYaw.IsEnabled && _controls.IsHeld(EliteAction.YawRight);

    // The rising edge of the stick's missile controls, or the keyboard's
    // one-shot. Read once per update, because reading is what advances the
    // remembered state.
    // The stick is read before the key, not after: || would short-circuit
    // past the edge on any update the key was pressed, and the stick's
    // state would go unrecorded.
    // A one-shot command, from a binding that may be a key, a button or a
    // trigger. The map's own WasPressed covers the first two; a trigger is
    // an axis with no press to consume, so its edge is made here.
    //
    // Both halves are read every update, and neither short-circuits the
    // other: WasPressed consumes, and the Edge only sees a rising edge if
    // it is shown every update's state.
    private bool WasPressed(EliteAction action, Edge edge)
    {
        bool held = edge.Pressed(_controls.IsHeld(action));

        return _controls.WasPressed(action) || held;
    }

    private bool WantsFireMissile() => WasPressed(EliteAction.FireMissile, _fireMissile);

    private bool WantsTargetMissile() => WasPressed(EliteAction.TargetMissile, _targetMissile);

    private bool WantsUntargetMissile() => WasPressed(EliteAction.UntargetMissile, _untargetMissile);

    private bool WantsEcm() => WasPressed(EliteAction.Ecm, _ecm);

    private bool WantsWarpJump() => WasPressed(EliteAction.WarpJump, _warpJump);

    private bool WantsHyperspace() => WasPressed(EliteAction.Hyperspace, _hyperspace);

    private void HandleFlightControls()
    {
        if (_controls.IsHeld(EliteAction.FireLaser))
        {
            _gameState.DrawLasers = _combat.FireLaser();
        }

        HandlePitchControls();
        HandleRollControls();
        HandleYawControls();

        HandleSpeedControls();
    }

    // A throttle lever is a position, so it sets the speed outright rather
    // than nudging it: where the lever is set is how fast the ship goes.
    // Only the SideWinder has one; every other device keeps the buttons,
    // and so does the keyboard.
    private void HandleSpeedControls()
    {
        if (_gameState.IsDocked)
        {
            return;
        }

        float? throttle = _controls.Throttle(EliteAxis.Speed);

        if (throttle.HasValue)
        {
            _ship.Speed = throttle.Value * _ship.MaxSpeed;
            return;
        }

        if (_controls.IsHeld(EliteAction.SpeedUp))
        {
            _ship.IncreaseSpeed();
        }

        if (_controls.IsHeld(EliteAction.SlowDown))
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
    private bool ApplyAxis(EliteAxis axis, float maxRate, bool keysActive, Action<float> setRate)
    {
        if (keysActive || !_controls.IsAnalog(axis))
        {
            return false;
        }

        setRate(_controls.Deflection(axis) * maxRate);
        return true;
    }

    // The keyboard halves of the flight controls, apart from the stick's,
    // so an analog axis can tell whether the pilot is using the keys
    // instead. Key-only on purpose: a pushed stick also holds its direction
    // action, and asking about that would have the stick suppress itself.
    private bool RollKeysHeld()
        => _controls.IsKeyHeld(EliteAction.RollLeft) || _controls.IsKeyHeld(EliteAction.RollRight);

    private bool PitchKeysHeld()
        => _controls.IsKeyHeld(EliteAction.PitchUp) || _controls.IsKeyHeld(EliteAction.PitchDown);

    private bool YawKeysHeld()
        => _controls.IsKeyHeld(EliteAction.YawLeft) || _controls.IsKeyHeld(EliteAction.YawRight);

    // Pitch up and down, the stick's own sense: SDL's Y is positive
    // downwards and so is the ship's pitch, so the deflection carries
    // straight across without a sign flip.
    private void HandlePitchControls()
    {
        if (ApplyAxis(EliteAxis.Pitch, _ship.MaxPitch, PitchKeysHeld(), rate => _ship.Pitch = rate))
        {
            _ship.IsPitching = true;
            return;
        }

        if (_controls.IsHeld(EliteAction.PitchUp))
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

        if (_controls.IsHeld(EliteAction.PitchDown))
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
        // Negated here rather than in the file: a stick pushed right is
        // positive where a roll to the right is a negative rate, and that
        // is a fact about Elite, not about the stick.
        if (ApplyAxis(EliteAxis.Roll, -_ship.MaxRoll, RollKeysHeld(), rate => _ship.Roll = rate))
        {
            _ship.IsRolling = true;
            return;
        }

        if (_controls.IsHeld(EliteAction.RollLeft))
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

        if (_controls.IsHeld(EliteAction.RollRight))
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
            && ApplyAxis(EliteAxis.Yaw, _ship.MaxYaw, YawKeysHeld(), rate => _ship.Yaw = rate))
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
        if (_controls.WasPressed(EliteAction.DockingComputerOn) &&
            !_gameState.IsDocked
            && _ship.HasDockingComputer)
        {
            EngageDockingComputer();
        }

        if (_controls.WasPressed(EliteAction.DockingComputerOff))
        {
            _pilot.DisengageAutoPilot();
        }

        HandleDockingComputerButton();

        if (WantsHyperspace() && (!_gameState.IsDocked))
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

        if (WantsWarpJump() &&
            (!_gameState.IsDocked)
            && (!_gameState.InWitchspace))
        {
            _space.JumpWarp();
        }

        if (_controls.WasPressed(EliteAction.Pause))
        {
            _gameState.IsGamePaused = true;
        }

        if (_controls.WasPressed(EliteAction.EscapeCapsule) &&
            (!_gameState.IsDocked)
            && _ship.HasEscapeCapsule
            && (!_gameState.InWitchspace))
        {
            _gameState.SetView(Screen.EscapeCapsule);
        }
    }

    // One button for what the keyboard spends two keys on: the stick has
    // one to spare, and which half is meant is never in doubt - if the
    // autopilot is flying, the only thing left to want is it to stop.
    private void HandleDockingComputerButton()
    {
        if (!_dockingComputer.Pressed(_controls.IsHeld(EliteAction.DockingComputerToggle)))
        {
            return;
        }

        if (_pilot.IsAutoPilotOn)
        {
            _pilot.DisengageAutoPilot();
        }
        else if (!_gameState.IsDocked && _ship.HasDockingComputer)
        {
            EngageDockingComputer();
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
        if (WantsEcm() &&
            !_gameState.IsDocked
            && _ship.HasECM)
        {
            _combat.ActivateECM(true);
        }

        if (WantsFireMissile() &&
            !_gameState.IsDocked)
        {
            _combat.FireMissile();
        }

        if (WantsTargetMissile() &&
            !_gameState.IsDocked)
        {
            _combat.ArmMissile();
        }

        if (WantsUntargetMissile() &&
            !_gameState.IsDocked)
        {
            _combat.UnarmMissile();
        }

        if (_controls.WasPressed(EliteAction.EnergyBomb) &&
            (!_gameState.IsDocked)
            && _ship.HasEnergyBomb)
        {
            _gameState.DetonateBomb = true;
            _ship.HasEnergyBomb = false;
        }

        if (_controls.WasPressed(EliteAction.EscapeCapsule) &&
            (!_gameState.IsDocked)
            && _ship.HasEscapeCapsule
            && (!_gameState.InWitchspace))
        {
            _gameState.SetView(Screen.EscapeCapsule);
        }
    }

    // A control that must act once per press, from a source that only says
    // whether it is down. Held, it fires on the first update and no other.
    private sealed class Edge
    {
        private bool _wasHeld;

        internal bool Pressed(bool held)
        {
            bool pressed = held && !_wasHeld;
            _wasHeld = held;

            return pressed;
        }
    }
}
