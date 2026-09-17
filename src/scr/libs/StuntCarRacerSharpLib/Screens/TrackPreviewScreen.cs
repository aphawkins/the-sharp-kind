// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind;
using SharpKind.Abstraction;
using SharpKind.Graphics;
using SharpKind.Input;
using StuntCarRacerSharpLib.Rendering;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Screens;

// The opponent drives the selected track, watched from the middle of the
// world (original HandleTrackPreview / CalcTrackPreviewViewpoint).
internal sealed class TrackPreviewScreen : IGameScreen, ILayerDrawer
{
    private readonly Race _race;
    private readonly LayerRunner _layers;
    private readonly IKeyboard _keyboard;
    private readonly IGamepad _gamepad;
    private readonly ScreenManager<GameMode, IGameScreen> _screens;
    private readonly IGraphics _graphics;
    private readonly ScreenLayout _screen;
    private readonly ScrPalette _palette;

    internal TrackPreviewScreen(
        Race race,
        IKeyboard keyboard,
        IGamepad gamepad,
        ScreenManager<GameMode, IGameScreen> screens,
        IGraphics graphics,
        ScreenLayout screen,
        ScrPalette palette)
    {
        _race = race;
        _keyboard = keyboard;
        _gamepad = gamepad;
        _screens = screens;
        _graphics = graphics;
        _screen = screen;
        _palette = palette;
        _layers = race.Layers(showOpponent: true, showPlayer: false, overlay: this);
    }

    public void Reset()
    {
        // reset the cars so the opponent is shown during the preview
        _race.Car.Reset();
        _race.Opponent.StartRace();
    }

    public void Update()
    {
        // Read before the physics gate: IsPressed is one-shot, and physics only runs every FrameGap ticks, so a press could otherwise land on an idle tick and be missed.
        if (_keyboard.IsPressed(ConsoleKey.S) || _gamepad.IsPressed(GamepadButton.A))
        {
            _screens.Set(GameMode.GameInProgress);
            return;
        }

        if (_keyboard.IsPressed(ConsoleKey.M) || _gamepad.IsPressed(GamepadButton.B))
        {
            _screens.Set(GameMode.TrackMenu);
            return;
        }

        if (!_race.PhysicsDue())
        {
            return;
        }

        _race.FrameMoved = true;
        _race.Opponent.Update();
        _race.Bridge.Move(_race.Car.CurrentPiece, _race.Opponent.CurrentPiece, _race.Opponent);

        const long centre = (long)Track.TrackCubes * Track.CubeSize / 2;

        // the Draw Bridge requires a higher viewpoint
        long viewY = _race.Track.Id == TrackId.DrawBridge
            ? _race.Opponent.Y - (5L * Track.CubeSize / 2)
            : _race.Opponent.Y - ((long)Track.CubeSize / 2);

        long viewX = centre + ((_race.Opponent.X - centre) / 2);
        long viewZ = centre + ((_race.Opponent.Z - centre) / 2);

        _race.Camera.LookAt(viewX, viewY, viewZ, _race.Opponent.X, _race.Opponent.Y, _race.Opponent.Z);
    }

    public void Draw() => _layers.Draw();

    // Explicit so it can't be mistaken for Draw() above, which draws the whole frame not just the preview overlay.
    void ILayerDrawer.Draw()
    {
        FastColor yellow = _palette.Colour(Track.ScrBaseColour + 3);
        float height = _screen.ScreenHeight;

        _graphics.DrawTextLeft(
            new(2, height - 130),
            $"Selected track - {_race.Track.Name}.",
            StuntCarRacerMain.SmallFont,
            yellow);
        _graphics.DrawTextLeft(
            new(2, height - 114),
            "Press 'S' to start game, 'M' for track menu, Escape to quit",
            StuntCarRacerMain.SmallFont,
            yellow);

        _graphics.DrawTextLeft(
            new(2, height - 82),
            "Keyboard controls during game :-",
            StuntCarRacerMain.SmallFont,
            yellow);
        _graphics.DrawTextLeft(
            new(2, height - 66),
            "  Left/Right arrows = Steer, Up = Accelerate, Down = Brake",
            StuntCarRacerMain.SmallFont,
            yellow);
        _graphics.DrawTextLeft(
            new(2, height - 50),
            "  Space = Boost, N = Change scenery",
            StuntCarRacerMain.SmallFont,
            yellow);
        _graphics.DrawTextLeft(
            new(2, height - 34),
            "  M = Back to track menu, Escape = Quit",
            StuntCarRacerMain.SmallFont,
            yellow);

        // Only worth the line when there is something to press it on.
        if (_gamepad.IsConnected)
        {
            _graphics.DrawTextLeft(
                new(2, height - 18),
                "  Stick = Steer/Accelerate/Brake, Buttons 1 or 3 = Boost",
                StuntCarRacerMain.SmallFont,
                yellow);
        }
    }
}
