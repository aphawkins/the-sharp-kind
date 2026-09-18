// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using BubbleBobbleSharp.Abstractions.Renditions;
using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Graphics;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Abstraction;
using SharpKind.Assets;
using SharpKind.Audio;
using SharpKind.Graphics;
using SharpKind.Input;

[assembly: CLSCompliant(false)]
[assembly: InternalsVisibleTo("BubbleBobbleSharpLib.Tests")]

namespace BubbleBobbleSharpLib;

/// <summary>
/// The game, as the host and the composition root see it. It puts a level on
/// the screen, steps to the next one on N, and closes on Escape; see
/// docs/bb-port-plan.md for what comes next.
/// </summary>
public sealed class BubbleBobbleMain : IGame, IGameApp
{
    // The C64 runs off the PAL raster interrupt, so one tick is one frame. Every counter
    // translated out of the reference is measured in these.
    internal const int TickRate = 50;

    // The level the game opens on. There is no front end yet to choose another.
    private const int FirstLevel = 1;

    // $0956, game-loop.s: both players start a game with three lives.
    private const int StartingLives = 3;

    // The row check_player_state puts a player on as a life starts. $04E1 writes it to $C2 for
    // either of them. A player at $2C/$DD stands with their feet exactly on the floor, which is the
    // cheapest check that the position bytes and the drawing offsets agree.
    private const byte SpawnY = 0xDD;

    // $B2. One is a player alive and in play - see docs/bb-port-plan.md, which settles it by
    // reading the byte in a running game rather than by inferring it.
    private const byte PlayingState = 0x01;

    // There is no front end to choose two players with, so the game starts the one it can name.
    // Player two's slot stays empty, which is what an unjoined second player looks like.
    private const int PlayingPlayers = 1;

    // Steps to the next level, for looking at levels there is no way yet to reach: nothing advances
    // a level until Phase 7 translates the progression, and a hundred levels are of no use if only
    // the first can be seen. It goes when the front end arrives. F12 is the host's frame dump, so
    // the two together photograph any level asked for.
    private const ConsoleKey NextLevelKey = ConsoleKey.N;

    // $A735, read at $04E5: the two players start a life at opposite ends of the same row.
    private static readonly byte[] s_spawnX = [0x2C, 0xEC];

    // $A737, read at $04C0: player one starts facing right and player two facing left.
    private static readonly byte[] s_spawnFrame = [0x00, 0x04];

    // $05C5. Player one's sprite is colour 5 and player two's is colour 3.
    private static readonly byte[] s_spawnColour = [0x05, 0x03];

    private readonly IAbstraction _abstraction;
    private readonly LevelStore _levels;
    private readonly IView<PlayfieldModel> _playfieldView;
    private readonly IView<SidebarModel> _sidebarView;
    private readonly IView<PlayerModel> _playerView;
    private readonly IView<HudModel> _hudView;

    // The players' own bytes and the ones they share with everything else that moves. Nothing drives
    // them yet: the routine that walks a player each frame is $D_1E6C's state dispatch, which
    // Phase 4 has not settled - see docs/bb-port-plan.md. What they hold is a player placed where a
    // life starts, so that the view has something true to draw.
    private readonly PlayerTable _playerTable = new();
    private readonly EntityTable _entities = new();
    private readonly LayerRunner _layers;

    // What the HUD shows. $0969 clears both scores and the high score when a game starts, and
    // nothing has scored yet: there is no player to score with until Phase 4.
    private readonly HudModel _hud = new(
        new byte[HudModel.ScoreBytes],
        new byte[HudModel.ScoreBytes],
        new byte[HudModel.ScoreBytes],
        StartingLives,
        StartingLives);

    // What the level in play looks like, rebuilt when the level changes rather than per frame: a
    // level's characters are settled the moment setup_level_screen has run.
    private PlayfieldModel _playfield;
    private SidebarModel _sidebar;

    public BubbleBobbleMain(
        IAbstraction abstraction,
        IAssetLocator assetLocator,
        IBbRendition rendition,
        LevelStore levels)
        : this(abstraction, assetLocator, rendition, levels, new())
    {
    }

    public BubbleBobbleMain(
        IAbstraction abstraction,
        IAssetLocator assetLocator,
        IBbRendition rendition,
        LevelStore levels,
        AudioOptions audioOptions)
    {
        ArgumentNullException.ThrowIfNull(abstraction);
        ArgumentNullException.ThrowIfNull(assetLocator);
        ArgumentNullException.ThrowIfNull(rendition);
        ArgumentNullException.ThrowIfNull(levels);
        ArgumentNullException.ThrowIfNull(audioOptions);

        _abstraction = abstraction;
        Graphics = abstraction.Graphics;
        Layout = abstraction.Layout;
        Keyboard = abstraction.Keyboard;
        Gamepad = abstraction.Gamepad;
        Sound = abstraction.Sound;
        AudioOptions = audioOptions;
        _levels = levels;

        BbViewSurface surface = new(Graphics, Layout, assetLocator);
        _playfieldView = rendition.CreatePlayfieldView(surface);
        _sidebarView = rendition.CreateSidebarView(surface);
        _playerView = rendition.CreatePlayerView(surface);
        _hudView = rendition.CreateHudView(surface);

        ShowLevel(FirstLevel);

        // Three bands, in the order the reference draws them: the level, the decoration written
        // over its outermost columns, and the HUD in the columns the level does not reach. The
        // level is trimmed to what survives the decoration, which is why the playfield view can
        // draw all 32 columns without knowing the sidebar exists.
        (Vector2 interior, float interiorWidth, float height) = surface.Layout.PlayfieldInterior;
        (Vector2 whole, float wholeWidth, _) = surface.Layout.PlayfieldArea;
        (Vector2 hud, float hudWidth, _) = surface.Layout.HudArea;

        _layers = new LayerRunner(
            Graphics,
            new RenderLayer(interior, interiorWidth, height, new PlayfieldLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new SidebarLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new PlayerLayer(this)),
            new RenderLayer(hud, hudWidth, height, new HudLayer(this)));
    }

    public bool IsRunning { get; private set; } = true;

    internal IGraphics Graphics { get; }

    internal ScreenLayout Layout { get; }

    internal IKeyboard Keyboard { get; }

    internal IGamepad Gamepad { get; }

    internal ISound Sound { get; }

    internal AudioOptions AudioOptions { get; }

    // Which level is on screen, counted the way the game counts them.
    internal int CurrentLevel { get; private set; }

    public void Run() => GameHost.Run(_abstraction, this, TickRate, TickRate);

    public void Update()
    {
        if (Keyboard.IsPressed(ConsoleKey.Escape))
        {
            IsRunning = false;
        }

        // Wraps, because the hundredth level's neighbour has to be something and the first is the
        // only level this game can name without a progression to ask.
        if (Keyboard.IsPressed(NextLevelKey))
        {
            ShowLevel(CurrentLevel == LevelStore.Count ? FirstLevel : CurrentLevel + 1);
        }
    }

    public void Draw()
    {
        Graphics.Clear();
        _layers.Draw();
        Graphics.ScreenUpdate();
    }

    // setup_level_screen, as far as this port has translated it: what the level's screen holds, and
    // what its border is decorated with. Both are settled once and then drawn every frame.
    [MemberNotNull(nameof(_playfield), nameof(_sidebar))]
    internal void ShowLevel(int number)
    {
        Level level = _levels.Level(number);

        _playfield = Playfield.Build(level);
        _sidebar = Sidebars.Select(level);
        CurrentLevel = number;

        StartPlayers();
    }

    // $04BB and $05C5, as far as a player who is only drawn needs them: where a life starts, which
    // way the player faces, what colour they are, and the three counters a level start puts back to
    // $FF. The rest of both routines - the music, the invincibility timer, the lives - belongs to
    // the phases that read those bytes, and is not translated here.
    private void StartPlayers()
    {
        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            bool playing = player < PlayingPlayers;

            _playerTable.State[player] = playing ? PlayingState : (byte)0;
            _playerTable.X[player] = s_spawnX[player];
            _playerTable.Y[player] = SpawnY;

            _entities.Frame[player] = s_spawnFrame[player];
            _entities.Colour[player] = s_spawnColour[player];

            // $04C4 to $04CB. None of the three is a count of anything yet: $FF is the value each
            // of them reads as "not happening", which is what a player standing still is.
            _entities.RiseCounter[player] = 0xFF;
            _entities.FallCounter[player] = 0xFF;
            _entities.GroundState[player] = 0xFF;
        }
    }

    // $1805, built fresh each frame rather than held. A level's characters are settled the moment it
    // is drawn, but a player's bytes are the ones that change every frame, so there is nothing here
    // to cache.
    private PlayerModel Players() => new(
        _playerTable.State,
        _playerTable.X,
        _playerTable.Y,
        _entities.Frame,
        _entities.Colour);

    // Layer 0, the level itself, trimmed to the columns the decoration does not cover.
    private sealed class PlayfieldLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._playfieldView.Draw(game._playfield);
    }

    // Layer 1, draw_border: the decoration down both edges, over the level already on the screen.
    private sealed class SidebarLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._sidebarView.Draw(game._sidebar);
    }

    // Layer 2, $1805: the players, over the level and the decoration both. A C64 sprite is in front
    // of the characters it passes, which is what this order stands in for.
    private sealed class PlayerLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._playerView.Draw(game.Players());
    }

    // Layer 3, $E3A7 and $046C: the scores and lives, in the columns the level never reaches.
    private sealed class HudLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._hudView.Draw(game._hud);
    }
}
