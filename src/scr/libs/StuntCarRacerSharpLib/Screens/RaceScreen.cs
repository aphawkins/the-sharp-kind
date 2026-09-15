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

        // the reference clears the per-car freezes when a race starts
        // (`StuntCarRacer.cpp:1254`, `:1312`)
        _race.PlayerPaused = false;
        _race.OpponentPaused = false;
    }

    public void Update()
    {
        // 'P' pauses and 'O' resumes, as the remake does
        // (`StuntCarRacer.cpp:1743-1749`). They are two keys rather than one
        // toggle, so a repeated press is harmless and no key-down latch is
        // needed alongside IsPressed's one-shot read.
        if (_keyboard.IsPressed(ConsoleKey.P))
        {
            _paused = true;
        }

        if (_keyboard.IsPressed(ConsoleKey.O))
        {
            _paused = false;
        }

        // The pad has no second button to spare for resuming, so Start is the
        // toggle the two keys deliberately are not. IsPressed is one-shot, so
        // a held Start cannot flip the pause every tick.
        if (_gamepad.IsPressed(GamepadButton.Start))
        {
            _paused = !_paused;
        }

        // 'M' abandons the race and returns to the track menu, as the remake
        // does (`StuntCarRacer.cpp:1731-1741`). The menu screen's Reset does
        // the rest of what the reference does there - clearing the opponent
        // and stopping the engine sound - and the drawbridge reset already
        // happens when the next race starts.
        if (_keyboard.IsPressed(ConsoleKey.M) || _gamepad.IsPressed(GamepadButton.Back))
        {
            _screens.Set(GameMode.TrackMenu);
            return;
        }

        // Backspace swaps the cockpit for a chase camera behind the car
        // (`StuntCarRacer.cpp:1727-1729`). The reference builds that key
        // into debug builds only; this port ships its debug keys (F5-F10)
        // unconditionally, so this one is no different. Like 'R' it sits
        // ahead of the paused return, so the view can be changed while the
        // race is frozen.
        if (_keyboard.IsPressed(ConsoleKey.Backspace))
        {
            _race.OutsideView = !_race.OutsideView;
        }

        // 'R' points the car the opposite way, so a car facing backwards can
        // recover (`StuntCarRacer.cpp:1039-1045`). The reference accepts it
        // whenever a race is in progress, pause included, so it sits ahead of
        // the paused return here and takes effect on the next physics frame.
        if (_keyboard.IsPressed(ConsoleKey.R))
        {
            _race.Car.TurnAround();
        }

        if (_paused)
        {
            // Silences the engine and freezes everything the race advances:
            // the physics, the drawbridge, and the tick the lap and result
            // timers count. StopLoop is idempotent, so calling it every
            // paused tick costs nothing - the reference calls
            // StopEngineSound the same way (`StuntCarRacer.cpp:1001-1004`).
            _sound.StopLoop();
            return;
        }

        // The full-rate part of the race (the original FramesWheelsEngine
        // call plus the race-finished timing, which the original drove from
        // the wall clock).
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

        // One physics frame of the race (every FrameGap ticks).
        // F6 freezes the player by skipping CarBehaviour outright; F7 freezes
        // the opponent inside its own update, which still tracks the distance
        // between the cars (`StuntCarRacer.cpp:1056-1071`,
        // `Opponent_Behaviour.cpp:376-383`).
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

    // The HUD layer's content. Explicitly implemented so it cannot be
    // mistaken for Draw() above: that one draws the whole frame, this one
    // draws only what goes over the world.
    void ILayerDrawer.Draw() => _race.DrawHud(gameOver: false);

    // ptitSeb's stuntcarremake keyboard controls: Left/Right arrows =
    // steer, Up = accelerate, Down = brake, Space = boost (applies with
    // either accelerate or brake held). Uses IsHeld rather than IsPressed:
    // these are continuous controls polled every physics tick, not
    // one-shot menu actions, so they must reflect whether the key is
    // physically down rather than being consumed after the first read
    // (IsPressed's one-shot consumption meant driving felt "stuck" as
    // soon as a second key was held, since a held key's state was cleared
    // on the previous tick and nothing but a fresh SDL key-repeat event —
    // which the OS doesn't reliably send per-key once several keys are
    // down at once — would set it again).
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

        // The pad is only read when the keyboard is idle, as the remake does
        // (Car_Behaviour.cpp:791), so keyboard driving is unaffected.
        return input == CarInput.None ? GamepadControls.ReadCarInput(_gamepad) : input;
    }
}
