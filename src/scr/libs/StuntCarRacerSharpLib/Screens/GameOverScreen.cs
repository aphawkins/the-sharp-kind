// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Graphics;
using SharpKind.Input;

namespace StuntCarRacerSharpLib.Screens;

// The original freezes the action and silences the engine once the game is
// over; M returns to the track menu.
internal sealed class GameOverScreen : IGameScreen, ILayerDrawer
{
    private readonly Race _race;
    private readonly LayerRunner _layers;
    private readonly IKeyboard _keyboard;
    private readonly IGamepad _gamepad;
    private readonly ISound _sound;
    private readonly ScreenManager<GameMode, IGameScreen> _screens;

    internal GameOverScreen(
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

    public void Reset() => _sound.StopLoop();

    public void Update()
    {
        if (_keyboard.IsPressed(ConsoleKey.M) || _gamepad.IsPressed(GamepadButton.A))
        {
            _screens.Set(GameMode.TrackMenu);
        }
    }

    public void Draw() => _layers.Draw();

    // The HUD layer's content. Explicitly implemented so it cannot be
    // mistaken for Draw() above: that one draws the whole frame, this one
    // draws only what goes over the world.
    void ILayerDrawer.Draw() => _race.DrawHud(gameOver: true);
}
