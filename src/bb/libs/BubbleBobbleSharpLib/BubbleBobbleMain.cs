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

/// <summary>The game, as the host and the composition root see it.</summary>
public sealed class BubbleBobbleMain : IGame, IGameApp
{
    internal const int TickRate = 25;

    private const int FirstLevel = 1;

    private const int StartingLives = 3;

    private const int PlayingPlayers = 1;

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

    private readonly GameLoop _loop;

    private readonly Input _input;
    private readonly LayerRunner _layers;

    private readonly byte[] _highScore = new byte[HudModel.ScoreBytes];

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

    internal int CurrentLevel { get; private set; }

    public void Run() => GameHost.Run(_abstraction, this, TickRate, TickRate);

    public void Update()
    {
        if (Keyboard.IsPressed(ConsoleKey.Escape))
        {
            IsRunning = false;
        }

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

    [MemberNotNull(nameof(_playfield), nameof(_sidebar))]
    internal void ShowLevel(int number)
    {
        Level level = _levels.Level(number);

        _playfield = Playfield.Build(level);
        _sidebar = Sidebars.Select(level);
        CurrentLevel = number;

        _loop.Start(level, _zones.Zones(number), number);
    }

    private SpriteModel Sprites() => new(
        _loop.Entities.State,
        _loop.Entities.X,
        _loop.Entities.Y,
        _loop.Entities.Frame,
        _loop.Entities.SpriteBase,
        _loop.Entities.Colour,
        _loop.Entities.FlashTimer,
        _loop.Entities.SpriteEnable);

    private HudModel Hud() => new(
        _loop.Scores.Bytes[..HudModel.ScoreBytes],
        _loop.Scores.Bytes[HudModel.ScoreBytes..],
        _highScore,
        StartingLives,
        StartingLives);

    private ObjectsModel Objects() => new(
        _loop.Pop.Drawn,
        _loop.Objects.Column,
        _loop.Objects.Row,
        _loop.Pop.DrawnSubY,
        _playfield.Colours);

    private ItemsModel Items() => new(
        _loop.Items.Type,
        _loop.Items.Column,
        _loop.Items.Row,
        _loop.Items.Art,
        _loop.Items.Colour,
        _playfield.Colours);

    private sealed class PlayfieldLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._playfieldView.Draw(game._playfield);
    }

    private sealed class ItemLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._itemView.Draw(game.Items());
    }

    private sealed class ObjectLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._objectView.Draw(game.Objects());
    }

    private sealed class SidebarLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._sidebarView.Draw(game._sidebar);
    }

    private sealed class SpriteLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._spriteView.Draw(game.Sprites());
    }

    private sealed class HudLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._hudView.Draw(game.Hud());
    }
}
