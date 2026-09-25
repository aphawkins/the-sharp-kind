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
using SharpKind;
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
    // One tick is one pass of the game loop, which waits for two of the PAL raster interrupt's 50
    // frames each time round - see GameLoop.
    internal const int TickRate = 25;

    // The level the game opens on. There is no front end yet to choose another.
    private const int FirstLevel = 1;

    // $0956, game-loop.s: both players start a game with three lives.
    private const int StartingLives = 3;

    // There is no front end to choose two players with, so the game starts the one it can name.
    // Player two's slot stays empty, which is what an unjoined second player looks like.
    private const int PlayingPlayers = 1;

    // Steps to the next level, for looking at levels there is no way yet to reach: nothing advances
    // a level until Phase 7 translates the progression, and a hundred levels are of no use if only
    // the first can be seen. It goes when the front end arrives. F12 is the host's frame dump, so
    // the two together photograph any level asked for.
    private const ConsoleKey NextLevelKey = ConsoleKey.N;

    private readonly IAbstraction _abstraction;
    private readonly LevelStore _levels;
    private readonly ZoneStore _zones;
    private readonly IView<PlayfieldModel> _playfieldView;
    private readonly IView<SidebarModel> _sidebarView;
    private readonly IView<SpriteModel> _spriteView;
    private readonly IView<HudModel> _hudView;
    private readonly IView<ObjectsModel> _objectView;
    private readonly IView<ItemsModel> _itemView;

    // Everything that moves, and the routines that move it, in the reference's order.
    private readonly GameLoop _loop;

    private readonly Input _input;
    private readonly LayerRunner _layers;

    // $0969 clears the high score when a game starts. Nothing translated sets it yet.
    private readonly byte[] _highScore = new byte[HudModel.ScoreBytes];

    // What the level in play looks like, rebuilt when the level changes rather than per frame: a
    // level's characters are settled the moment setup_level_screen has run.
    private PlayfieldModel _playfield;
    private SidebarModel _sidebar;

    public BubbleBobbleMain(
        IAbstraction abstraction,
        IAssetLocator assetLocator,
        IBbRendition rendition,
        LevelStore levels,
        ZoneStore zones)
        : this(abstraction, assetLocator, rendition, levels, zones, new())
    {
    }

    public BubbleBobbleMain(
        IAbstraction abstraction,
        IAssetLocator assetLocator,
        IBbRendition rendition,
        LevelStore levels,
        ZoneStore zones,
        AudioOptions audioOptions)
    {
        ArgumentNullException.ThrowIfNull(abstraction);
        ArgumentNullException.ThrowIfNull(assetLocator);
        ArgumentNullException.ThrowIfNull(rendition);
        ArgumentNullException.ThrowIfNull(levels);
        ArgumentNullException.ThrowIfNull(zones);
        ArgumentNullException.ThrowIfNull(audioOptions);

        _abstraction = abstraction;
        Graphics = abstraction.Graphics;
        Layout = abstraction.Layout;
        Keyboard = abstraction.Keyboard;
        Gamepad = abstraction.Gamepad;
        Sound = abstraction.Sound;
        AudioOptions = audioOptions;
        _levels = levels;
        _zones = zones;

        _input = new Input(Keyboard, Gamepad);

        _loop = new GameLoop(new BbRandom(new RandomSource(Random.Shared)), PlayingPlayers);

        BbViewSurface surface = new(Graphics, Layout, assetLocator);
        _playfieldView = rendition.CreatePlayfieldView(surface);
        _sidebarView = rendition.CreateSidebarView(surface);
        _spriteView = rendition.CreateSpriteView(surface);
        _hudView = rendition.CreateHudView(surface);
        _objectView = rendition.CreateObjectView(surface);
        _itemView = rendition.CreateItemView(surface);

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
            new RenderLayer(interior, interiorWidth, height, new ItemLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new ObjectLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new SidebarLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new SpriteLayer(this)),
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

        _loop.Pass(_input.Read());
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

        _loop.Start(level, _zones.Zones(number), number);
    }

    // $1805, built fresh each frame rather than held. A level's characters are settled the moment it
    // is drawn, but a sprite's bytes are the ones that change every frame, so there is nothing here
    // to cache.
    private SpriteModel Sprites() => new(
        _loop.Entities.State,
        _loop.Entities.X,
        _loop.Entities.Y,
        _loop.Entities.Frame,
        _loop.Entities.SpriteBase,
        _loop.Entities.Colour,
        _loop.Entities.FlashTimer,
        _loop.Entities.SpriteEnable);

    // $E3A7 and $046C: both scores from $0400, the high score and the lives.
    private HudModel Hud() => new(
        _loop.Scores.Bytes[..HudModel.ScoreBytes],
        _loop.Scores.Bytes[HudModel.ScoreBytes..],
        _highScore,
        StartingLives,
        StartingLives);

    // $E90E, built fresh each frame: what BubblePop drew this pass, and where.
    private ObjectsModel Objects() => new(
        _loop.Pop.Drawn,
        _loop.Objects.Column,
        _loop.Objects.Row,
        _loop.Objects.SubY,
        _playfield.Colours);

    // $1844's L_1934: the level's two items, as characters, with their colour RAM from L_186B.
    private ItemsModel Items() => new(
        _loop.Items.Type,
        _loop.Items.Column,
        _loop.Items.Row,
        _loop.Items.Art,
        _loop.Items.Colour,
        _playfield.Colours);

    // Layer 0, the level itself, trimmed to the columns the decoration does not cover.
    private sealed class PlayfieldLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._playfieldView.Draw(game._playfield);
    }

    // Layer 0 still, $1844: the food and the special item are characters of the level, so they sit
    // in it, under the bubbles - see docs/bb-port-plan.md, item 2e.
    private sealed class ItemLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._itemView.Draw(game.Items());
    }

    // Layer 1, $E90E: the bubbles and the pop animation, over the level and under everything else -
    // see docs/bb-port-plan.md, item 2c.
    private sealed class ObjectLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._objectView.Draw(game.Objects());
    }

    // Layer 2, draw_border: the decoration down both edges, over the level already on the screen.
    private sealed class SidebarLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._sidebarView.Draw(game._sidebar);
    }

    // Layer 3, $1805: the players and the enemies, over the level and the decoration both. A C64
    // sprite is in front of the characters it passes, which is what this order stands in for.
    private sealed class SpriteLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._spriteView.Draw(game.Sprites());
    }

    // Layer 4, $E3A7 and $046C: the scores and lives, in the columns the level never reaches.
    private sealed class HudLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._hudView.Draw(game.Hud());
    }
}
