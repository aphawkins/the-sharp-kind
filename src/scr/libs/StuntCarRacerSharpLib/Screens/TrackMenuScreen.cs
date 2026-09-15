// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using System.Globalization;
using System.Numerics;
using SharpKind;
using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Graphics;
using SharpKind.Input;
using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Rendering;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Screens;

// The track list and orbiting camera (original HandleTrackMenu /
// CalcTrackMenuViewpoint). Not throttled by the frame gap.
//
// The original's HandleTrackMenu is plain text over the 3D world (the
// Bitmap/menu.png background art in stuntcarremake is never actually
// loaded by that game's code). This drawn overlay is a cosmetic addition
// beyond strict porting: menu.png's centre panel is transparent, so the
// orbiting track view still shows through, with the track list positioned
// inside the panel.
internal sealed class TrackMenuScreen : IGameScreen, ILayerDrawer
{
    private const string MenuImage = "Menu";

    // menu.png is 320x200; its centre panel (inset from the measured
    // transparent bounds to clear the wooden frame/logo) in that space.
    private const float CanvasWidth = 320f;
    private const float PanelLeft = 38f;
    private const float PanelTop = 70f;
    private const float PanelBottom = 190f;
    private const float RowHeight = 11f;

    private readonly Race _race;
    private readonly LayerRunner _layers;
    private readonly IKeyboard _keyboard;
    private readonly IGamepad _gamepad;
    private readonly ISound _sound;
    private readonly ScreenManager<GameMode, IGameScreen> _screens;
    private readonly IGraphics _graphics;
    private readonly ScreenLayout _screen;
    private readonly ScrPalette _palette;
    private int _orbitAngle;

    // The stick is a continuous reading, so one flick would otherwise step
    // through every track it is held past. Only a change of direction moves
    // the selection.
    private int _lastSteer;

    internal TrackMenuScreen(
        Race race,
        IKeyboard keyboard,
        IGamepad gamepad,
        ISound sound,
        ScreenManager<GameMode, IGameScreen> screens,
        IGraphics graphics,
        ScreenLayout screen,
        ScrPalette palette)
    {
        _race = race;
        _keyboard = keyboard;
        _gamepad = gamepad;
        _sound = sound;
        _screens = screens;
        _graphics = graphics;
        _screen = screen;
        _palette = palette;

        // the original only shows the opponent outside the track menu
        _layers = race.Layers(showOpponent: false, showPlayer: false, overlay: this);
    }

    // The menu has no opponent and no engine: the reference clears the
    // opponent and stops the engine sound on the way in
    // (`StuntCarRacer.cpp:1731-1741`). Doing it here covers every route to
    // the menu, whether from the game-over screen or mid-race on 'M'.
    public void Reset()
    {
        _sound.StopLoop();
        _race.Opponent.Clear();
    }

    public void Update()
    {
        _orbitAngle = (_orbitAngle + 128) & (Track.MaxAngle - 1);

        const long centre = (long)Track.TrackCubes * Track.CubeSize / 2;
        const long radius = (Track.TrackCubes - 2) * (long)Track.CubeSize / AmigaTrig.Precision;

        long viewX = centre + (AmigaTrig.Sin(_orbitAngle) * radius);
        const long viewY = -3L * Track.CubeSize;
        long viewZ = centre + (AmigaTrig.Cos(_orbitAngle) * radius);

        _race.Camera.LookAt(viewX, viewY, viewZ, centre, 0, centre);

        // track selection
        for (ConsoleKey key = ConsoleKey.D1; key <= ConsoleKey.D8; key++)
        {
            if (_keyboard.IsPressed(key) && _race.Track.Id != (TrackId)(key - ConsoleKey.D1))
            {
                _race.LoadTrack((TrackId)(key - ConsoleKey.D1));
            }
        }

        SelectTrackWithGamepad();

        if (_keyboard.IsPressed(ConsoleKey.S) || _gamepad.IsPressed(GamepadButton.A))
        {
            _screens.Set(GameMode.TrackPreview);
        }
    }

    public void Draw() => _layers.Draw();

    // The layer-2 content: what this screen draws over the world.
    // Explicitly implemented so it cannot be mistaken for Draw()
    // above, which draws the whole frame.
    void ILayerDrawer.Draw()
    {
        // menu.png's frame draws over the world; its centre panel is
        // transparent so the orbiting track view still shows through
        _graphics.DrawImagePart(
            MenuImage,
            Vector2.Zero,
            _screen.ScreenSize,
            Vector2.Zero,
            new(CanvasWidth, 200f));

        float scale = _screen.ScreenWidth / CanvasWidth;
        FastColor yellow = _palette.Colour(Track.ScrBaseColour + 3);
        FastColor white = _palette.Colour(Track.ScrBaseColour + 15);

        _graphics.DrawTextLeft(new(PanelLeft * scale, PanelTop * scale), "Choose track :-", StuntCarRacerMain.SmallFont, yellow);

        for (int i = 0; i < 8; i++)
        {
            string name = (TrackId)i switch
            {
                TrackId.LittleRamp => "Little Ramp",
                TrackId.SteppingStones => "Stepping Stones",
                TrackId.HumpBack => "Hump Back",
                TrackId.BigRamp => "Big Ramp",
                TrackId.SkiJump => "Ski Jump",
                TrackId.DrawBridge => "Draw Bridge",
                TrackId.HighJump => "High Jump",
                _ => "Roller Coaster",
            };

            FastColor colour = _race.Track.Id == (TrackId)i ? white : yellow;
            float y = PanelTop + RowHeight + (i * RowHeight);
            _graphics.DrawTextLeft(
                new(PanelLeft * scale, y * scale),
                string.Create(CultureInfo.InvariantCulture, $"'{i + 1}' -  {name}"),
                StuntCarRacerMain.SmallFont,
                colour);
        }

        _graphics.DrawTextLeft(
            new(PanelLeft * scale, (PanelBottom - 18) * scale),
            $"Current track - {_race.Track.Name}.",
            StuntCarRacerMain.SmallFont,
            yellow);
        _graphics.DrawTextLeft(
            new(PanelLeft * scale, (PanelBottom - 6) * scale),
            "Press 'S' to select, Escape to quit",
            StuntCarRacerMain.SmallFont,
            yellow);
    }

    // Left/right steps through the track list, the pad equivalent of the
    // number keys. The list does not wrap, so the ends are the ends.
    private void SelectTrackWithGamepad()
    {
        int steer = GamepadControls.Steer(_gamepad);
        int previous = _lastSteer;
        _lastSteer = steer;

        if (steer is 0 || steer == previous)
        {
            return;
        }

        TrackId selected = (TrackId)Math.Clamp((int)_race.Track.Id + steer, (int)TrackId.LittleRamp, (int)TrackId.RollerCoaster);
        if (selected != _race.Track.Id)
        {
            _race.LoadTrack(selected);
        }
    }
}
