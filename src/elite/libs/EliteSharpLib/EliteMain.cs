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

    // What this tick decided the two flight overlays should say, captured at
    // the moment it decided - see UpdateInFlight for why they cannot simply
    // be read again while the frame is composed. Cleared at the top of every
    // tick, so a docked or paused frame shows neither.
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

        // The three bands a frame is, furthest first. Built once rather than
        // per tick: the set never changes, and a band with nothing to draw
        // this tick draws nothing rather than leaving the list.
        //
        // The window region is the opening the cockpit art leaves for the
        // universe to be seen through, and ViewLayout's viewport already is
        // exactly that - derived from the rendition's own scanner height.
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

    // Exposed (GameState itself stays internal) for headless test harnesses
    // that need to observe screen/docked/game-over state without a rendered
    // frame.
    internal GameState State { get; }

    // What one call to Update is worth in game time. Read rather than fixed:
    // GameLoop is a fixed timestep, so an update is always worth the same
    // amount of time, but which amount is now the commander's setting.
    private float SecondsPerUpdate => 1f / State.Config.Engine.Graphics.Fps;

    // One rate for both halves. The game used to simulate at a fixed 13.5Hz
    // and present at the configured rate, which meant most presents
    // redisplayed a frame nothing had changed. It now simulates and composes
    // at that one rate, because every rate it was written with - the
    // housekeeping, the motion, the animations - is expressed per second
    // rather than per update, so the game plays the same however often it is
    // asked to.
    public void Run()
    {
        float fps = State.Config.Engine.Graphics.Fps;
        GameHost.Run(_abstraction, this, fps, fps);
    }

    // One fixed-rate game tick: move the game on, paint what it now looks
    // like, then read the controls.
    //
    // Simulate and Compose were one method until 2026-08-26, because Elite's
    // update drew the universe as it moved it (as The New Kind did). They
    // are separated so the two can eventually run at rates of their own;
    // both still run exactly once per tick here, and the frame is unchanged.
    //
    // Input stays a third phase at the end because that is where this port
    // has always read it: moving it ahead of Compose would show a screen
    // change a tick earlier than it does today. Which phase it really
    // belongs to is the last frame-rate item's question, not this one's.
    public void Update()
    {
        // Applied every update rather than once at startup: the commander
        // can change it on the settings screen, and a stick can be plugged
        // in after the game has begun. It is a string assignment - which
        // device that names is worked out where the devices are known.
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

    // Everything the tick changes. Returns false when the game is paused,
    // which is also the signal not to compose: the framebuffer is left
    // alone so the paused frame stays on screen.
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

    // Everything the tick draws, in the order the frame is built up: the
    // starfield behind the universe, the universe behind the view's own
    // chrome, and the console across the bottom of all of it.
    private void Compose()
    {
        _draw.SetFullScreenClipRegion();
        _graphics.Clear();
        _layers.Draw();
    }

    // The part of a tick that only applies while flying: laser cooling,
    // messages, the hyperspace countdown and the MCount-driven housekeeping.
    //
    // The two overlays are recorded rather than drawn, and that is not
    // tidiness - it is the whole reason the fields exist. Both say something
    // about the middle of the tick that is no longer true at the end of it:
    // the countdown is shown before it is decremented, and the message is
    // the one already on screen, not the "ENERGY LOW" this same method may
    // raise a few lines further down. Compose cannot read either back.
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

    // One step of the MCount clock and the jobs hung off it.
    //
    // The order is the original's and the phases are load-bearing: the
    // hyperspace countdown and the docking-computer reminder read the count
    // *before* it moves, everything below reads it after. Swapping either
    // side of the decrement shifts which tick those jobs land on.
    private void Housekeeping()
    {
        if (_space.IsHyperspaceReady && (State.MCount & 3) == 0)
        {
            _space.CountdownHyperspace();
        }

        // Moved here from beside the autopilot itself, which still steers
        // every update: the reminder is a periodic message, so it belongs on
        // the clock rather than firing on every frame the count happens to
        // be sitting on a multiple of 128.
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

    // The intro screens take only their own keys. They own the title music,
    // and they stop it on the way out; a chart or options key that moved the
    // view straight off them left the music playing behind the game.
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

    // Ctrl-M: cycle the mission briefings, which normal play puts hours away.
    // One key rather than five. Ctrl-modified because the briefings are read
    // while docked and a bare M would fire from the commander-name screens,
    // which take typed letters; F12 was the obvious key but GameHost has it
    // for frame dumps. Off unless MissionJump's environment variable is set -
    // see MissionJump for what each jump costs.
    private void HandleMissionJumpKeys()
    {
        // Ctrl first, and held rather than pressed: IsPressed(M) consumes the
        // press, so testing M first ate the bare M that fires a missile, and
        // consuming Ctrl here would take it from Ctrl-H's galactic hyperspace.
        if (!MissionJump.IsEnabled ||
            !_keyboard.IsHeld(ConsoleModifiers.Control) ||
            !_keyboard.IsPressed(ConsoleKey.M))
        {
            return;
        }

        MissionJump.To(State, _planet, _missionJumpStage);
        _missionJumpStage = (_missionJumpStage + 1) % MissionJump.Count;
    }

    // F1 - F4, or the stick's hat, which double as the docked screens.
    //
    // Chained as else-if, where the keys alone did not need to be: a hat
    // pushed to a corner reports both of its directions at once, and two
    // SetView calls in one tick would load a view only to throw it away -
    // each one resets its screen and flips the starfield.
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

    // Layer 1, the universe: ships, sun, planet, and the break pattern -
    // which is seen through the canopy like the rest of them, so it draws
    // here rather than with the cockpit over it.
    private sealed class UniverseLayer(EliteMain game) : ILayerDrawer
    {
        public void Draw()
        {
            game._space.DrawUniverse();
            game.State.CurrentView.DrawUniverse();
        }
    }

    // Layer 2, the HUD: the screen the commander is on, the two flight
    // overlays and the console, over the other two and across the whole
    // display. The console draws last because it is the nearest thing there
    // is.
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
