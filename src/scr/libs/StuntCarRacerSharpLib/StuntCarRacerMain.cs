// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using System.Runtime.CompilerServices;
using SharpKind;
using SharpKind.Abstraction;
using SharpKind.Assets;
using SharpKind.Audio;
using SharpKind.Graphics;
using SharpKind.Input;
using StuntCarRacerSharpLib.Rendering;
using StuntCarRacerSharpLib.Screens;
using StuntCarRacerSharpLib.Tracks;

[assembly: CLSCompliant(false)]
[assembly: InternalsVisibleTo("StuntCarRacerSharpLib.Tests")]

namespace StuntCarRacerSharpLib;

public sealed class StuntCarRacerMain : IGame, IGameApp
{
    // Original remake ticks OnFrameMove at 50Hz: input and engine sound run at the full rate, physics steps every FrameGap ticks
    // (DEFAULT_FRAME_GAP = 4, i.e. 12.5Hz; the Amiga original used MIN.FRAMES = 6).
    internal const int TickRate = 50;

    internal const string SmallFont = "Small";
    internal const string LargeFont = "Large";

    private readonly IAbstraction _abstraction;

    private bool _sceneryKeyDown;

    public StuntCarRacerMain(IAbstraction abstraction, IAssetLocator assetLocator)
        : this(abstraction, assetLocator, TrackId.LittleRamp, new(), new RandomSource(Random.Shared))
    {
    }

    public StuntCarRacerMain(IAbstraction abstraction, IAssetLocator assetLocator, TrackId trackId)
        : this(abstraction, assetLocator, trackId, new(), new RandomSource(Random.Shared))
    {
    }

    public StuntCarRacerMain(IAbstraction abstraction, IAssetLocator assetLocator, AudioOptions audioOptions)
        : this(abstraction, assetLocator, TrackId.LittleRamp, audioOptions, new RandomSource(Random.Shared))
    {
    }

    public StuntCarRacerMain(
        IAbstraction abstraction,
        IAssetLocator assetLocator,
        AudioOptions audioOptions,
        IRandomSource randomSource)
        : this(abstraction, assetLocator, TrackId.LittleRamp, audioOptions, randomSource)
    {
    }

    // Locator arrives alongside the abstraction, not inside it: it carries the configured system tier, and assets (the palette today) must come from that tier's set.
    private StuntCarRacerMain(
        IAbstraction abstraction,
        IAssetLocator assetLocator,
        TrackId trackId,
        AudioOptions audioOptions,
        IRandomSource randomSource)
    {
        ArgumentNullException.ThrowIfNull(abstraction);
        ArgumentNullException.ThrowIfNull(assetLocator);
        ArgumentNullException.ThrowIfNull(audioOptions);
        ArgumentNullException.ThrowIfNull(randomSource);

        _abstraction = abstraction;
        Graphics = abstraction.Graphics;
        Layout = abstraction.Layout;
        Keyboard = abstraction.Keyboard;
        Gamepad = abstraction.Gamepad;
        Sound = abstraction.Sound;
        Palette = new(assetLocator);

        // Effect cooldowns in physics frames (12.5Hz), sized to sample lengths so a repeated trigger doesn't stack in the mixer.
        // OffRoad/Wreck share a cooldown and buffer as in the original. Pan (and HitCar's fixed volume) mirror DSSetMode
        // (StuntCarRacer.cpp:166-230): engine/Smash on the left, everything else on the right.
        SfxSample offRoadOrWreck = new(10, pan: 1f);
        AudioController audio = new(
            Sound,
            new Dictionary<string, SfxSample>
            {
                { "Grounded", new(4, pan: 1f) },
                { "Creak", new(6, pan: 1f) },
                { "Smash", new(11, pan: -1f) },
                { "OffRoad", offRoadOrWreck },
                { "Wreck", offRoadOrWreck },
                { "HitCar", new(9, volume: 56f / 64f, pan: 1f) },
            },
            audioOptions);

        Race = new(Graphics, Layout, Palette, Sound, audio, trackId, randomSource);

        Screens = new(Keyboard);
        Screens.Add(GameMode.TrackMenu, new TrackMenuScreen(Race, Keyboard, Gamepad, Sound, Screens, Graphics, Layout, Palette));
        Screens.Add(GameMode.TrackPreview, new TrackPreviewScreen(Race, Keyboard, Gamepad, Screens, Graphics, Layout, Palette));
        Screens.Add(GameMode.GameInProgress, new RaceScreen(Race, Keyboard, Gamepad, Sound, Screens));
        Screens.Add(GameMode.GameOver, new GameOverScreen(Race, Keyboard, Gamepad, Sound, Screens));
        Screens.Set(GameMode.TrackMenu);
    }

    public bool IsRunning { get; private set; } = true;

    // The game mode state machine: TrackMenu -> TrackPreview -> GameInProgress -> GameOver.
    internal ScreenManager<GameMode, IGameScreen> Screens { get; }

    internal IKeyboard Keyboard { get; }

    internal IGamepad Gamepad { get; }

    internal ISound Sound { get; }

    internal IGraphics Graphics { get; }

    internal ScreenLayout Layout { get; }

    internal ScrPalette Palette { get; }

    internal Race Race { get; }

    public void Run() => GameHost.Run(_abstraction, this, TickRate, TickRate);

    // One 50Hz tick (the original OnFrameMove).
    public void Update()
    {
        Race.FrameMoved = false;

        if (Keyboard.IsPressed(ConsoleKey.Escape))
        {
            IsRunning = false;
            return;
        }

        // N cycles the scenery type, as the original menu option
        if (Keyboard.IsPressed(ConsoleKey.N))
        {
            if (!_sceneryKeyDown)
            {
                Race.NextScenery();
                _sceneryKeyDown = true;
            }
        }
        else
        {
            _sceneryKeyDown = false;
        }

        // F5/F6/F7 are the remake's dev aids (StuntCarRacer.cpp:1706-1716): toggles rather than the race pause's two keys, as the reference has them.
        if (Keyboard.IsPressed(ConsoleKey.F5))
        {
            Race.ShowStats = !Race.ShowStats;
        }

        if (Keyboard.IsPressed(ConsoleKey.F6))
        {
            Race.PlayerPaused = !Race.PlayerPaused;
        }

        if (Keyboard.IsPressed(ConsoleKey.F7))
        {
            Race.OpponentPaused = !Race.OpponentPaused;
        }

        // F9/F10 tune the physics frame gap live, as the original did; F9 never drops below every tick, F10 is unbounded upwards.
        if (Keyboard.IsPressed(ConsoleKey.F9) && Race.FrameGap > 1)
        {
            Race.FrameGap--;
        }

        if (Keyboard.IsPressed(ConsoleKey.F10))
        {
            Race.FrameGap++;
        }

        Screens.Current.Update();
    }

    public void Draw()
    {
        Screens.Current.Draw();

        if (Race.ShowStats)
        {
            Race.DrawStats();
        }

        Graphics.ScreenUpdate();
    }
}
