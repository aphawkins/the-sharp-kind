// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Graphics;
using SharpKind.Input;
using StuntCarRacerSharpLib.Cars;

namespace StuntCarRacerSharpLib.Screens;

// The race itself (original GAME_IN_PROGRESS): input, engine sound and race
// timing at the full tick rate, car physics every FrameGap ticks.
internal sealed class RaceScreen : IGameScreen, ILayerDrawer
{
    private readonly Race _race;
    private readonly LayerRunner _layers;
    private readonly IKeyboard _keyboard;
    private readonly IGamepad _gamepad;
    private readonly ISound _sound;
    private readonly ScreenManager<GameMode, IGameScreen> _screens;

    private bool _paused;

    internal RaceScreen(
        Race race,
        IKeyboard keyboard,
        IGamepad gamepad,
        ISound sound,
        ScreenManager<GameMode, IGameScreen> screens)
    {
        _race = race;
        _keyboard = keyboard;
        _gamepad = gamepad;
        _sound = sound;
        _screens = screens;
        _layers = race.Layers(showOpponent: true, showPlayer: true, overlay: this);
    }

    public void Reset()
    {
        _race.Car.StartRace();
        _race.Car.BoostReserve = _race.Track.StandardBoost;
        _race.Opponent.StartRace();
        _race.Bridge.Reset(_race.Opponent);

        _race.RaceTick = 0;
        _race.RaceFinished = false;
        _race.RaceWon = false;
        _race.RaceFinishedTick = 0;
        _paused = false;

        // the reference clears the per-car freezes when a race starts (StuntCarRacer.cpp:1254, :1312)
        _race.PlayerPaused = false;
        _race.OpponentPaused = false;
    }

    public void Update()
    {
        // 'P' pauses, 'O' resumes, as the remake does (StuntCarRacer.cpp:1743-1749); two keys rather than a toggle, so repeats are harmless.
        if (_keyboard.IsPressed(ConsoleKey.P))
        {
            _paused = true;
        }

        if (_keyboard.IsPressed(ConsoleKey.O))
        {
            _paused = false;
        }

        // Start toggles pause (the pad has no spare button for a second key); IsPressed is one-shot so holding it can't flip every tick.
        if (_gamepad.IsPressed(GamepadButton.Start))
        {
            _paused = !_paused;
        }

        // 'M' abandons the race, as the remake does (StuntCarRacer.cpp:1731-1741); the menu screen's Reset clears the opponent and engine sound.
        if (_keyboard.IsPressed(ConsoleKey.M) || _gamepad.IsPressed(GamepadButton.Back))
        {
            _screens.Set(GameMode.TrackMenu);
            return;
        }

        // Backspace swaps to a chase camera (StuntCarRacer.cpp:1727-1729; debug-only there, but this port ships debug keys unconditionally).
        // Sits ahead of the paused return, so the view can change while the race is frozen.
        if (_keyboard.IsPressed(ConsoleKey.Backspace))
        {
            _race.OutsideView = !_race.OutsideView;
        }

        // 'R' turns the car around to recover (StuntCarRacer.cpp:1039-1045); accepted even while paused, so it sits ahead of the paused return.
        if (_keyboard.IsPressed(ConsoleKey.R))
        {
            _race.Car.TurnAround();
        }

        if (_paused)
        {
            // StopLoop is idempotent, so calling it every paused tick costs nothing, as the reference's StopEngineSound does (StuntCarRacer.cpp:1001-1004).
            _sound.StopLoop();
            return;
        }

        // Original FramesWheelsEngine call plus race-finished timing, which the original drove from the wall clock.
        _race.RaceTick++;
        _race.Car.ApplyEngineRevs();
        _race.UpdateEngineSound();

        // show the race result for six seconds, then it is game over
        if (_race.RaceFinished && _race.RaceTick - _race.RaceFinishedTick > 6 * StuntCarRacerMain.TickRate)
        {
            _screens.Set(GameMode.GameOver);
            return;
        }

        if (!_race.PhysicsDue())
        {
            return;
        }

        // One physics frame of the race (every FrameGap ticks). F6 skips CarBehaviour outright to freeze the player; F7 freezes the
        // opponent inside its own update, which still tracks distance between the cars (StuntCarRacer.cpp:1056-1071, Opponent_Behaviour.cpp:376-383).
        _race.FrameMoved = true;
        if (!_race.PlayerPaused)
        {
            _race.Car.Update(ReadInput());
        }

        _race.Opponent.Update(_race.OpponentPaused);
        _race.Bridge.Move(_race.Car.CurrentPiece, _race.Opponent.CurrentPiece, _race.Opponent);
        _race.Car.UpdateLapData();
        _race.Opponent.UpdateLapData();
        _race.Car.UpdateDamage();
        _race.UpdateCamera();

        // the race finishes when either car completes the final lap
        if (!_race.RaceFinished && (_race.Car.RaceFinished || _race.Opponent.LapNumber >= 4))
        {
            _race.RaceFinished = true;
            _race.RaceWon = _race.Opponent.CalculateIfWinning() < 0;
            _race.RaceFinishedTick = _race.RaceTick;
        }

        _race.UpdateSounds();
    }

    public void Draw() => _layers.Draw();

    // Explicit so it can't be mistaken for Draw() above, which draws the whole frame not just the HUD overlay.
    void ILayerDrawer.Draw() => _race.DrawHud(gameOver: false);

    // ptitSeb's stuntcarremake mapping: arrows steer, Up accelerate, Down brake, Space boost.
    // IsHeld not IsPressed: these are continuous per-tick controls, not one-shot menu actions.
    private CarInput ReadInput()
    {
        CarInput input = CarInput.None;

        if (_keyboard.IsHeld(ConsoleKey.LeftArrow))
        {
            input |= CarInput.Left;
        }

        if (_keyboard.IsHeld(ConsoleKey.RightArrow))
        {
            input |= CarInput.Right;
        }

        if (_keyboard.IsHeld(ConsoleKey.UpArrow))
        {
            input |= CarInput.Accelerate;
        }

        if (_keyboard.IsHeld(ConsoleKey.DownArrow))
        {
            input |= CarInput.Brake;
        }

        if (_keyboard.IsHeld(ConsoleKey.Spacebar))
        {
            input |= CarInput.Boost;
        }

        // Pad only read when the keyboard is idle, as the remake does (Car_Behaviour.cpp:791).
        return input == CarInput.None ? GamepadControls.ReadCarInput(_gamepad) : input;
    }
}
