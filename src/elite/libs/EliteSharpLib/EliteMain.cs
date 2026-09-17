// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Audio;
using EliteSharpLib.Conflict;
using EliteSharpLib.Controls;
using EliteSharpLib.Graphics;
using EliteSharpLib.Save;
using EliteSharpLib.Ships;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Graphics;
using SharpKind.Input;

[assembly: CLSCompliant(false)]

// For unit testing
[assembly: InternalsVisibleTo("EliteSharpLib.Tests")]
[assembly: InternalsVisibleTo("EliteSharpLib.Fakes")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

// For benchmarking
[assembly: InternalsVisibleTo("EliteSharpLib.Benchmarks")]

// For test renderering
[assembly: InternalsVisibleTo("EliteSharpLib.Renderer")]

namespace EliteSharpLib;

public sealed class EliteMain : IGame, IGameApp
{
    private readonly IAbstraction _abstraction;
    private readonly IGraphics _graphics;
    private readonly IKeyboard _keyboard;
    private readonly IGamepad _gamepad;
    private readonly EliteControlMap _controls;

    private readonly AudioController _audio;
    private readonly PlanetController _planet;
    private readonly Combat _combat;
    private readonly IBaseView _baseView;
    private readonly IEliteDraw _draw;
    private readonly List<long> _framesDrawn = [];
    private readonly LayerRunner _layers;
    private readonly Pilot _pilot;
    private readonly SaveFile _save;
    private readonly ScannerController _scanner;
    private readonly PlayerShip _ship;
    private readonly Space _space;
    private readonly Stars _stars;
    private readonly Universe _universe;

    // Which mission screen the next Ctrl-M jumps to (HandleMissionJumpKeys).
    private int _missionJumpStage;

    // What this tick decided the two flight overlays should say; see UpdateInFlight for
    // why they can't be read again while composing. Cleared each tick.
    private string? _pendingMessage;

    /// <inheritdoc cref="_pendingMessage"/>
    private int? _pendingCountdown;

    internal EliteMain(
        IAbstraction abstraction,
        EliteControlMap controls,
        GameState gameState,
        PlayerShip ship,
        IEliteDraw draw,
        IBaseView baseView,
        Universe universe,
        Stars stars,
        Pilot pilot,
        Combat combat,
        SaveFile save,
        Space space,
        ScannerController scanner,
        AudioController audio,
        PlanetController planet)
    {
        ArgumentNullException.ThrowIfNull(abstraction);
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(universe);
        ArgumentNullException.ThrowIfNull(stars);
        ArgumentNullException.ThrowIfNull(pilot);
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(space);
        ArgumentNullException.ThrowIfNull(scanner);
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(planet);

        _planet = planet;
        _abstraction = abstraction;
        _graphics = abstraction.Graphics;
        _keyboard = abstraction.Keyboard;
        _gamepad = abstraction.Gamepad;
        _controls = controls;
        _audio = audio;
        State = gameState;
        _ship = ship;
        _draw = draw;
        _baseView = baseView;
        _universe = universe;
        _stars = stars;
        _pilot = pilot;
        _combat = combat;
        _save = save;
        _space = space;
        _scanner = scanner;

        // The three bands a frame is, furthest first, built once since the set never changes. The
        // window region is the opening the cockpit art leaves for the universe, matching ViewLayout's viewport.
        _layers = new LayerRunner(
            _graphics,
            new RenderLayer(
                new(_draw.Layout.ViewportLeft, _draw.Layout.ViewportTop),
                _draw.Layout.ViewportWidth,
                _draw.Layout.ViewportHeight,
                new StarsLayer(this)),
            new RenderLayer(
                new(_draw.Layout.ViewportLeft, _draw.Layout.ViewportTop),
                _draw.Layout.ViewportWidth,
                _draw.Layout.ViewportHeight,
                new UniverseLayer(this)),
            new RenderLayer(
                new(0, 0),
                _draw.Layout.ScreenWidth,
                _draw.Layout.ScreenHeight,
                new HudLayer(this)));
    }

    public bool IsRunning => !State.ExitGame;

    // Exposed (GameState itself stays internal) for headless test harnesses observing
    // screen/docked/game-over state without a rendered frame.
    internal GameState State { get; }

    // Read rather than fixed: the commander's fps setting decides how much game time an update is worth.
    private float SecondsPerUpdate => 1f / State.Config.Engine.Graphics.Fps;

    // Simulate and compose at the same configured rate: every rate the game logic was written
    // with is expressed per second, not per update, so it plays the same at any rate.
    public void Run()
    {
        float fps = State.Config.Engine.Graphics.Fps;
        GameHost.Run(_abstraction, this, fps, fps);
    }

    // One fixed-rate game tick: move the game on, paint what it now looks like, then read the
    // controls. Simulate and Compose are separate so each can eventually run at its own rate.
    public void Update()
    {
        // Applied every update, not once at startup: the commander can change it on the settings
        // screen, and a stick can be plugged in mid-game.
        _gamepad.PreferredDevice = State.Config.Engine.ActiveController;

        if (!Simulate())
        {
            return;
        }

        Compose();
        State.CurrentView.HandleInput();
    }

    // Present the frame composed by the last update. Runs at GameTickRate,
    // once per tick.
    public void Draw()
    {
        // keep only the presents from the last second, for the FPS display
        int stale = 0;
        long oneSecondAgo = Stopwatch.GetTimestamp() - Stopwatch.Frequency;
        while (stale < _framesDrawn.Count && _framesDrawn[stale] <= oneSecondAgo)
        {
            stale++;
        }

        _framesDrawn.RemoveRange(0, stale);
        _framesDrawn.Add(Stopwatch.GetTimestamp());

        _graphics.ScreenUpdate();
    }

    // Returns false when paused, the signal not to compose - the framebuffer stays as the paused frame.
    private bool Simulate()
    {
        InitialiseGame();
        State.Clock.BeginUpdate(SecondsPerUpdate);
        _audio.UpdateSound(State.Clock.Ticks);
        _ship.IsRolling = false;
        _ship.IsPitching = false;
        _ship.IsYawing = false;
        HandleViewKeys();

        if (State.IsGamePaused)
        {
            if (_keyboard.IsPressed(ConsoleKey.R))
            {
                State.IsGamePaused = false;
            }

            return false;
        }

        if (_ship.Energy < PlayerShip.EnergyMin)
        {
            State.GameOver();
        }

        if (State.MessageCount > 0)
        {
            State.MessageCount = MathF.Max(State.MessageCount - State.Clock.Ticks, 0);
        }

        _ship.LevelOut();

        if (_pilot.IsAutoPilotOn)
        {
            _pilot.AutoDock();
        }

        _pendingMessage = null;
        _pendingCountdown = null;

        State.CurrentView.Update();
        _space.MoveUniverse(State.Clock.Ticks);

        if (!State.IsDocked && !State.IsGameOver)
        {
            UpdateInFlight();
        }

        return true;
    }

    // Frame build order: starfield behind the universe, universe behind the view's own chrome, console on top.
    private void Compose()
    {
        _draw.SetFullScreenClipRegion();
        _graphics.Clear();
        _layers.Draw();
    }

    // Laser cooling, messages, hyperspace countdown, MCount-driven housekeeping. The two overlays
    // are recorded rather than drawn: they say something true at the middle of the tick, not
    // necessarily by the end - Compose cannot read either back.
    private void UpdateInFlight()
    {
        _combat.CoolLaser();

        if (State.MessageCount > 0)
        {
            _pendingMessage = State.MessageString;
        }

        if (_space.IsHyperspaceReady)
        {
            _pendingCountdown = _space.HyperCountdown;
        }

        // However many steps this update is worth. At the game's own rate
        // that is exactly one, as it has always been.
        int due = State.Clock.Advance();
        for (int step = 0; step < due; step++)
        {
            Housekeeping();
        }

        _combat.TimeECM();
    }

    // One step of the MCount clock and the jobs hung off it. Order is load-bearing: the
    // hyperspace countdown and docking-computer reminder read the count before it moves,
    // everything below reads it after.
    private void Housekeeping()
    {
        if (_space.IsHyperspaceReady && (State.MCount & 3) == 0)
        {
            _space.CountdownHyperspace();
        }

        // Reminder is periodic, so it belongs on the clock rather than firing every frame the count is a multiple of 128.
        if (_pilot.IsAutoPilotOn && (State.MCount & 127) == 0)
        {
            State.InfoMessage("Docking Computers On");
        }

        State.MCount--;
        if (State.MCount < 0)
        {
            State.MCount = 255;
        }

        if ((State.MCount & 7) == 0)
        {
            _ship.RegenerateShields();
        }

        if ((State.MCount & 31) == 10)
        {
            if (_ship.IsEnergyLow())
            {
                State.InfoMessage("ENERGY LOW");
                _audio.PlayEffect(nameof(SoundEffect.Beep));
            }

            _space.UpdateAltitude();
        }

        if ((State.MCount & 31) == 20)
        {
            _space.UpdateCabinTemp();
        }

        if ((State.MCount == 0) && (!State.InWitchspace))
        {
            _combat.RandomEncounter();
        }
    }

    // Intro screens own the title music and stop it on the way out, else it plays behind the game.
    private void HandleViewKeys()
    {
        if (State.CurrentScreen is Screen.IntroOne or Screen.IntroTwo)
        {
            return;
        }

        HandleFlightViewKeys();
        HandleChartViewKeys();
        HandleStatusViewKeys();
        HandleMissionJumpKeys();
    }

    // Ctrl-M: cycle the mission briefings, which normal play puts hours away. Ctrl-modified since
    // a bare M fires from commander-name screens and F12 is taken by GameHost's frame dumps. Off
    // unless MissionJump's environment variable is set; see MissionJump for what each jump costs.
    private void HandleMissionJumpKeys()
    {
        // Ctrl first, and held not pressed: testing M first would eat the bare M that fires a missile.
        if (!MissionJump.IsEnabled ||
            !_keyboard.IsHeld(ConsoleModifiers.Control) ||
            !_keyboard.IsPressed(ConsoleKey.M))
        {
            return;
        }

        MissionJump.To(State, _planet, _missionJumpStage);
        _missionJumpStage = (_missionJumpStage + 1) % MissionJump.Count;
    }

    // F1 - F4, or the stick's hat, which double as the docked screens. Chained as else-if: a hat
    // pushed to a corner reports both directions at once, and two SetView calls in one tick
    // would load a view only to throw it away.
    private void HandleFlightViewKeys()
    {
        if (_controls.WasPressed(EliteAction.FrontView))
        {
            State.SetView(State.IsDocked ? Screen.Undocking : Screen.FrontView);
        }
        else if (_controls.WasPressed(EliteAction.RearView) && !State.IsDocked)
        {
            State.SetView(Screen.RearView);
        }
        else if (_controls.WasPressed(EliteAction.LeftView) && !State.IsDocked)
        {
            State.SetView(Screen.LeftView);
        }
        else if (_controls.WasPressed(EliteAction.RightView))
        {
            State.SetView(State.IsDocked ? Screen.EquipShip : Screen.RightView);
        }
    }

    // F5 - F8: the charts and market
    private void HandleChartViewKeys()
    {
        if (_controls.WasPressed(EliteAction.GalacticChart))
        {
            State.SetView(Screen.GalacticChart);
        }

        if (_controls.WasPressed(EliteAction.ShortRangeChart))
        {
            State.SetView(Screen.ShortRangeChart);
        }

        if (_controls.WasPressed(EliteAction.PlanetData))
        {
            State.SetView(Screen.PlanetData);
        }

        if (_controls.WasPressed(EliteAction.MarketPrices) && (!State.InWitchspace))
        {
            State.SetView(Screen.MarketPrices);
        }
    }

    // F9 - F11: commander status, inventory and options
    private void HandleStatusViewKeys()
    {
        if (_controls.WasPressed(EliteAction.CommanderStatus))
        {
            State.SetView(Screen.CommanderStatus);
        }

        if (_controls.WasPressed(EliteAction.Inventory))
        {
            State.SetView(Screen.Inventory);
        }

        if (_controls.WasPressed(EliteAction.Options))
        {
            State.EnterOptions();
        }
    }

    /// <summary>
    /// Initialise the game parameters.
    /// </summary>
    private void InitialiseGame()
    {
        if (State.IsInitialised)
        {
            return;
        }

        State.Reset();
        _pilot.Reset();
        _ship.Reset();
        _combat.Reset();
        _save.GetLastSave();

        _ship.Speed = 1;
        _space.IsHyperspaceReady = false;
        State.IsGamePaused = false;

        _stars.CreateNewStars();
        _universe.ClearUniverse();
        _space.DockPlayer();

        State.SetView(Screen.IntroOne);
    }

    // Layer 0, the backdrop. Always furthest, always the window region.
    private sealed class StarsLayer(EliteMain game) : ILayerDrawer
    {
        public void Draw() => game._stars.Draw();
    }

    // Layer 1, the universe: ships, sun, planet, and the break pattern - seen through the canopy like the rest.
    private sealed class UniverseLayer(EliteMain game) : ILayerDrawer
    {
        public void Draw()
        {
            game._space.DrawUniverse();
            game.State.CurrentView.DrawUniverse();
        }
    }

    // Layer 2, the HUD: screen, flight overlays and console. Console draws last as the nearest thing.
    private sealed class HudLayer(EliteMain game) : ILayerDrawer
    {
        public void Draw()
        {
            game.State.CurrentView.Draw();

            if (game.State.Config.Engine.Graphics.ShowFps)
            {
                game._baseView.DrawFps(game._framesDrawn.Count);
            }

            if (game._pendingMessage is string message)
            {
                game._baseView.DrawInfoMessage(message);
            }

            if (game._pendingCountdown is int countdown)
            {
                game._baseView.DrawHyperspaceCountdown(countdown);
            }

            game._scanner.UpdateConsole();
        }
    }
}
