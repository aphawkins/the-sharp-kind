// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SharpKind;
using SharpKind.Audio;
using SharpKind.Graphics;
using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Rendering;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib;

// The track/car/opponent state and its world/HUD rendering: the part of the
// game screens actually drive, split out of StuntCarRacerMain (which keeps
// only the run loop, screen wiring and audio setup) so screens depend on
// this instead of the whole game object.
internal sealed class Race
{
    private const int DefaultFrameGap = 4;

    private readonly IGraphics _graphics;
    private readonly ScreenLayout _screen;
    private readonly ScrPalette _palette;
    private readonly ISound _sound;
    private readonly AudioController _audio;
    private readonly BackdropRenderer _backdrop;
    private readonly HudRenderer _hud;
    private readonly CarMesh _carMesh;
    private readonly RoadTextures _roadTextures;
    private readonly IRandomSource _randomSource;
    private readonly List<WorldPolygon> _worldPolygons = [];

    private TrackRenderer _renderer;
    private OpponentRenderer _opponentRenderer;
    private PlayerRenderer _playerRenderer;
    private int _frameCount;

    internal Race(
        IGraphics graphics,
        ScreenLayout screen,
        ScrPalette palette,
        ISound sound,
        AudioController audio,
        TrackId trackId,
        IRandomSource randomSource)
    {
        _graphics = graphics;
        _screen = screen;
        _palette = palette;
        _sound = sound;
        _audio = audio;
        _backdrop = new(graphics, screen, palette);
        _hud = new(graphics, screen);
        _carMesh = new(palette);
        _roadTextures = new(palette);
        _randomSource = randomSource;

        LoadTrack(trackId);
    }

    internal SceneCamera Camera { get; } = new();

    // The remake's bOutsideView: a chase camera behind the car instead of
    // the cockpit, toggled with Backspace (`StuntCarRacer.cpp:1727-1729`).
    internal bool OutsideView { get; set; }

    internal Track Track { get; private set; }

    internal CarPhysics Car { get; private set; }

    internal OpponentPhysics Opponent { get; private set; }

    internal DrawBridge Bridge { get; private set; }

    // How often the physics steps, per plan pass 3 step 1: made settable so
    // the frame gap can be tuned as the original's -/+ keys did.
    internal int FrameGap { get; set; } = DefaultFrameGap;

    // The remake's per-car debug freezes (bPlayerPaused / bOpponentPaused,
    // `StuntCarRacer.cpp:1710-1716`) and its stats overlay (bShowStats):
    // dev aids, toggled by F6/F7/F5, and reset when a race starts.
    internal bool PlayerPaused { get; set; }

    internal bool OpponentPaused { get; set; }

    internal bool ShowStats { get; set; }

    // Whether the last tick stepped the physics (the original bFrameMoved).
    internal bool FrameMoved { get; set; }

    // Race timing shared by the race and game-over screens: the tick count
    // since the race started, and when and how it finished.
    internal int RaceTick { get; set; }

    internal bool RaceFinished { get; set; }

    internal bool RaceWon { get; set; }

    internal int RaceFinishedTick { get; set; }

    internal void NextScenery() => _backdrop.NextSceneryType();

    // The original's frameCount countdown: the physics only steps every
    // FrameGap ticks.
    internal bool PhysicsDue()
    {
        if (_frameCount > 0)
        {
            _frameCount--;
        }

        if (_frameCount == 0)
        {
            _frameCount = FrameGap;
            return true;
        }

        return false;
    }

    [MemberNotNull(
        nameof(Track),
        nameof(Car),
        nameof(Opponent),
        nameof(Bridge),
        nameof(_renderer),
        nameof(_opponentRenderer),
        nameof(_playerRenderer))]
    internal void LoadTrack(TrackId trackId)
    {
        Track = Track.Load(trackId);
        Car = new(Track, _randomSource);
        Opponent = new(Track, Car, _randomSource);
        Bridge = new(Track);
        _renderer = new(Track, _graphics, _screen, _palette, _roadTextures);
        _opponentRenderer = new(Opponent, _carMesh, _palette);
        _playerRenderer = new(Car, _carMesh);
    }

    // Place the camera for the race: in the driver's seat, or behind the
    // car when the outside view is on.
    internal void UpdateCamera()
    {
        if (OutsideView)
        {
            Camera.ChaseCar(Car);
        }
        else
        {
            Camera.FollowCar(Car);
        }
    }

    // A screen's frame, as the three layers both games now draw: the
    // backdrop furthest, the world over it, and the screen's own cockpit or
    // text over both. SCR's window region is the whole display on every
    // screen it draws, so all three carry the same rectangle - the layers
    // are here for the ordering and the depth flush between them, not to
    // clip anything.
    //
    // Built once per screen and kept, because the set never changes: which
    // cars a screen shows is fixed when the screen is constructed.
    internal LayerRunner Layers(bool showOpponent, bool showPlayer, ILayerDrawer overlay)
        => new(
            _graphics,
            new RenderLayer(Vector2.Zero, _screen.ScreenWidth, _screen.ScreenHeight, new BackdropLayer(this)),
            new RenderLayer(
                Vector2.Zero,
                _screen.ScreenWidth,
                _screen.ScreenHeight,
                new WorldLayer(this, showOpponent, showPlayer)),
            new RenderLayer(Vector2.Zero, _screen.ScreenWidth, _screen.ScreenHeight, overlay));

    // The F5 stats overlay (the remake's bShowStats block,
    // `StuntCarRacer.cpp:1343-1361`). The reference printed DXUT's frame and
    // device stats, which mean nothing here; what is worth seeing instead is
    // the state the other debug keys change - the frame gap F9/F10 tune and
    // the per-car freezes F6/F7 toggle.
    internal void DrawStats()
    {
        FastColor white = _palette.Colour(Track.ScrBaseColour + 15);

        _graphics.DrawTextLeft(
            new(4f, 4f),
            $"Frame gap: {FrameGap} ({(float)StuntCarRacerMain.TickRate / FrameGap:0.#}Hz)",
            StuntCarRacerMain.SmallFont,
            white);

        _graphics.DrawTextLeft(
            new(4f, 24f),
            $"Player: {(PlayerPaused ? "frozen" : "running")}   Opponent: {(OpponentPaused ? "frozen" : "running")}",
            StuntCarRacerMain.SmallFont,
            white);
    }

    // Play the effect triggers from the physics, throttled by the shared
    // AudioController cooldowns. Runs once per physics frame.
    internal void UpdateSounds()
    {
        _audio.UpdateSound();

        if (Car.GroundedSoundTriggered)
        {
            _audio.PlayEffect("Grounded", Car.GroundedVolume, pitch: 1.0);
        }

        if (Car.CreakSoundTriggered)
        {
            _audio.PlayEffect("Creak", Car.CreakVolume, pitch: 1.0);
        }

        if (Car.SmashSoundTriggered)
        {
            _audio.PlayEffect("Smash");
        }

        if (Car.OffRoadSoundTriggered)
        {
            _audio.PlayEffect("OffRoad", volume: null, Car.OffRoadPitch);
        }
        else if (Car.WreckSoundTriggered)
        {
            _audio.PlayEffect("Wreck", volume: null, Car.WreckPitch);
        }

        if (Opponent.HitCarSoundTriggered)
        {
            _audio.PlayEffect("HitCar");
        }
    }

    // Engine sound sample and pitch, from the original FramesWheelsEngine.
    // The samples were recorded at 11025Hz; the original played them at
    // AMIGA_PAL_HZ / period.
    internal void UpdateEngineSound()
    {
        const int amigaPalHz = 3546895;
        const int sampleRate = 11025;

        int r = Car.EngineRevs + 378;
        int period = 4800000 / r;
        int index = 6;

        if (period >= 0x3fff)
        {
            period = 0x3ffe;
        }

        period |= Car.EngineFluctuation;
        if (period < 124)
        {
            period = 124; // lowest possible period
        }

        // calculate the sound index that will give period < 256
        while (period >= 256)
        {
            period >>= 1;
            --index;

            if (index < 0)
            {
                index = 0;
            }
        }

        double frequency = (double)amigaPalHz / period;
        string sample = index == 0 ? "TickOver" : $"EnginePitch{index + 1}";
        _sound.PlayLoop(sample, frequency / sampleRate);
    }

    // The in-game display: the cockpit overlay (wheels, engine, damage
    // crack/holes, speed bar, lap/boost/distance read-outs) plus the
    // remake's text overlays (opponent name at race start, race result).
    internal void DrawHud(bool gameOver)
    {
        FastColor white = _palette.Colour(Track.ScrBaseColour + 15);
        float height = _screen.ScreenHeight;

        // the cockpit belongs to the inside view; the outside view shows the
        // car itself and keeps only the text overlays
        if (!OutsideView)
        {
            DrawCockpit();
        }

        // output the opponent's name for four seconds at race start
        if (RaceTick < 4 * StuntCarRacerMain.TickRate)
        {
            _graphics.DrawTextCentre(height - 300, $"Opponent: {Opponent.Name}", StuntCarRacerMain.SmallFont, white);
        }

        if (!RaceFinished)
        {
            return;
        }

        if (gameOver)
        {
            _graphics.DrawTextCentre(height - 300, "GAME OVER: Press 'M' for track menu", StuntCarRacerMain.LargeFont, white);
        }
        else
        {
            // the result text flashes white/black, changing every half second
            int flash = (RaceTick - RaceFinishedTick) % StuntCarRacerMain.TickRate;
            FastColor colour = flash < StuntCarRacerMain.TickRate / 2 ? white : _palette.Colour(Track.ScrBaseColour);
            _graphics.DrawTextCentre(height - 300, RaceWon ? "RACE WON" : "RACE LOST", StuntCarRacerMain.LargeFont, colour);
        }
    }

    // The cockpit overlay: wheels, engine, damage crack/holes, speed bar and
    // the lap/boost/distance read-outs.
    private void DrawCockpit()
        => _hud.Draw(new(
            Car.LeftWheelFrame,
            Car.RightWheelFrame,
            Car.LeftWheelBounce,
            Car.RightWheelBounce,
            Car.BoostActivated != 0,
            Car.NewDamage,
            Car.SmashHoles,
            Car.DisplaySpeed,
            Car.LapNumber,
            Car.BoostReserve,
            Opponent.DistanceToPlayer(),
            Car.OnChains,
            Car.WaitingToReleaseChains,
            Car.ZAngle,
            Car.CurrentLapTicks,
            Car.BestLapTicks));

    // Layer 0, the backdrop: the sky and ground fill and the scenery
    // skyline. Clears the frame first, which is where that has always
    // happened - the backdrop is what paints over it.
    private sealed class BackdropLayer(Race race) : ILayerDrawer
    {
        public void Draw()
        {
            race._graphics.Clear();
            race._backdrop.Draw(race.Camera);
        }
    }

    // Layer 1, the world: the track, and the cars that are visible from
    // this screen. Everything here is depth-tested, and the runner flushes
    // that depth content before the layer above draws over it.
    private sealed class WorldLayer(Race race, bool showOpponent, bool showPlayer) : ILayerDrawer
    {
        public void Draw()
        {
            race._worldPolygons.Clear();
            if (showOpponent)
            {
                race._opponentRenderer.AppendWorldPolygons(race._worldPolygons);
            }

            // the player's car is only ever visible from the outside view -
            // the cockpit view sits inside it
            // (`StuntCarRacer.cpp:1600-1605`)
            if (showPlayer && race.OutsideView)
            {
                race._playerRenderer.AppendWorldPolygons(race._worldPolygons);
            }

            race._renderer.Draw(
                race.Camera,
                race._worldPolygons,
                race.Car.CurrentPiece,
                race.Car.CurrentSegment);
        }
    }
}
