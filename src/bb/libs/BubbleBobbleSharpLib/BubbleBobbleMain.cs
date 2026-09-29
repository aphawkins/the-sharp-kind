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
using BubbleBobbleSharpLib.Screens;
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

    private const int PlayingPlayers = 1;

    // `$3234`: where a room shows its clock, at the top, and each player's count, along the bottom, and in what colours.
    private const int RoomClockColumn = 15;
    private const int RoomClockColour = 9;
    private const int RoomCountRow = 24;
    private const int RoomSecondColumn = 30;
    private const int RoomFirstColour = 13;
    private const int RoomSecondColour = 11;

    private static readonly byte[] s_hiddenItems = [0xFF, 0xFF];

    private static readonly ConsoleKey[] s_superKeys =
        [ConsoleKey.S, ConsoleKey.U, ConsoleKey.P, ConsoleKey.O, ConsoleKey.R, ConsoleKey.Tab];

    private readonly IAbstraction _abstraction;
    private readonly ZoneStore _zones;
    private readonly IView<PlayfieldModel> _playfieldView;
    private readonly IView<SidebarModel> _sidebarView;
    private readonly IView<SpriteModel> _spriteView;
    private readonly IView<HudModel> _hudView;
    private readonly IView<ObjectsModel> _objectView;
    private readonly IView<ItemsModel> _itemView;
    private readonly IView<RoomModel> _roomView;
    private readonly IView<EffectsModel> _effectsView;
    private readonly IView<BannerModel> _bannerView;
    private readonly float _bannerHeight;
    private readonly ScreenManager<BbScreen, IGameScreen> _screens;

    private readonly Input _input;
    private readonly Random _random;

    private readonly byte[] _highScore = new byte[HudModel.ScoreBytes];

    private LevelStore _levels;
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
        : this(abstraction, assetLocator, rendition, levels, zones, audioOptions, Random.Shared)
    {
    }

    // The random the game rolls from, which a test seeds to have every run play the same.
    internal BubbleBobbleMain(
        IAbstraction abstraction,
        IAssetLocator assetLocator,
        IBbRendition rendition,
        LevelStore levels,
        ZoneStore zones,
        AudioOptions audioOptions,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(abstraction);
        ArgumentNullException.ThrowIfNull(assetLocator);
        ArgumentNullException.ThrowIfNull(rendition);
        ArgumentNullException.ThrowIfNull(levels);
        ArgumentNullException.ThrowIfNull(zones);
        ArgumentNullException.ThrowIfNull(audioOptions);
        ArgumentNullException.ThrowIfNull(random);

        _abstraction = abstraction;
        _random = random;
        Graphics = abstraction.Graphics;
        Layout = abstraction.Layout;
        Keyboard = abstraction.Keyboard;
        Gamepad = abstraction.Gamepad;
        Sound = abstraction.Sound;
        AudioOptions = audioOptions;
        _levels = levels;
        _zones = zones;

        _input = new Input(Keyboard, Gamepad);

        Loop = new GameLoop(new BbRandom(new RandomSource(random)), PlayingPlayers);

        BbViewSurface surface = new(Graphics, Layout, assetLocator);
        _playfieldView = rendition.CreatePlayfieldView(surface);
        _sidebarView = rendition.CreateSidebarView(surface);
        _spriteView = rendition.CreateSpriteView(surface);
        _hudView = rendition.CreateHudView(surface);
        _objectView = rendition.CreateObjectView(surface);
        _itemView = rendition.CreateItemView(surface);
        _roomView = rendition.CreateRoomView(surface);
        _effectsView = rendition.CreateEffectsView(surface);
        _bannerHeight = surface.Layout.BannerHeight;
        _bannerView = rendition.CreateBannerView(surface);

        ShowLevel(FirstLevel);

        _screens = CreateScreens(rendition.CreateTitleView(surface), CreateLayers(surface), CreateEndingLayers(surface));
    }

    public bool IsRunning { get; private set; } = true;

    internal IGraphics Graphics { get; }

    internal ScreenLayout Layout { get; }

    internal IKeyboard Keyboard { get; }

    internal IGamepad Gamepad { get; }

    internal ISound Sound { get; }

    internal AudioOptions AudioOptions { get; }

    internal int CurrentLevel { get; private set; }

    internal GameLoop Loop { get; private set; }

    internal BbScreen Screen => _screens.CurrentId;

    internal ReadOnlySpan<byte> HighScore => _highScore;

    public void Run() => GameHost.Run(_abstraction, this, TickRate, TickRate);

    public void Update()
    {
        if (Keyboard.IsPressed(ConsoleKey.Escape))
        {
            IsRunning = false;
        }

        _screens.Current.Update();
    }

    public void Draw()
    {
        Graphics.Clear();

        // The last frame's final layer left its clip set, and the banner sits outside every layer.
        Graphics.SetClipRegion(Vector2.Zero, Layout.ScreenWidth, Layout.ScreenHeight);
        _bannerView.Draw(new BannerModel(_bannerHeight));
        _screens.Current.Draw();
        Graphics.ScreenUpdate();
    }

    // $F005: a new game, from the first level. The title's tune stops when the game's theme starts.
    internal void StartGame() => StartGame(superMode: false);

    // $45E3-$462A: the cheat makes every level's enemies harder, and the data stays so.
    internal void StartGame(bool superMode)
    {
        if (superMode)
        {
            _levels = _levels.Super();
        }

        Loop = new GameLoop(new BbRandom(new RandomSource(_random)), PlayingPlayers);
        ShowLevel(FirstLevel);
        _screens.Set(BbScreen.Play);
    }

    // Goes to the game with the level it has, which is how a test sets one up.
    internal void Play() => _screens.Set(BbScreen.Play);

    [MemberNotNull(nameof(_playfield), nameof(_sidebar))]
    internal void ShowLevel(int number, bool next = false)
    {
        Level level = _levels.Level(number);

        _playfield = Playfield.Build(level);
        _sidebar = Sidebars.Select(level);
        CurrentLevel = number;

        Loop.Start(level, _zones.Zones(number), number, next);
    }

    private void UpdateGame()
    {
        // $0AA8: the game is won, and nothing but the ending goes on.
        if (Loop.Ending.Active)
        {
            Loop.Ending.Step(_input.Read());
            KeepHighScore();
            PlaySongs();

            if (Loop.Ending.Finished)
            {
                _screens.Set(BbScreen.Title);
            }

            return;
        }

        Loop.Pass(_input.Read());
        KeepHighScore();

        // $0A6A: a cleared level whose clock has run out goes on to the next.
        if (Loop.IsComplete && Loop.EndLevel() is int next)
        {
            ShowLevel(next, next: true);
        }

        // $7FB2: an item has skipped levels.
        if (Loop.SkipLevel() is int skipped)
        {
            ShowLevel(skipped, next: true);
        }

        ShowColours();
        PlaySongs();

        // $0AA3: with both players out and the wait over the game goes back to the title.
        if (Loop.Finished)
        {
            _screens.Set(BbScreen.Title);
        }
    }

    private ScreenManager<BbScreen, IGameScreen> CreateScreens(IView<TitleModel> titleView, LayerRunner layers, LayerRunner endingLayers)
    {
        ScreenManager<BbScreen, IGameScreen> screens = new(Keyboard);

        screens.Add(BbScreen.Title, new TitleScreen(TitleFire, SuperKeys, StartGame, PlaySong, titleView.Draw));
        screens.Add(BbScreen.Play, new PlayScreen(UpdateGame, () => (Loop.Ending.Blank ? endingLayers : layers).Draw()));
        screens.Set(BbScreen.Title);

        return screens;
    }

    // $A5B7: the ending's screen is blank but for its text and the giant.
    private LayerRunner CreateEndingLayers(BbViewSurface surface)
    {
        (Vector2 whole, float wholeWidth, float height) = surface.Layout.PlayfieldArea;

        return new LayerRunner(
            Graphics,
            new RenderLayer(whole, wholeWidth, height, new RoomLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new SpriteLayer(this)));
    }

    private LayerRunner CreateLayers(BbViewSurface surface)
    {
        (Vector2 interior, float interiorWidth, float height) = surface.Layout.PlayfieldInterior;
        (Vector2 whole, float wholeWidth, _) = surface.Layout.PlayfieldArea;
        (Vector2 hud, float hudWidth, _) = surface.Layout.HudArea;

        return new LayerRunner(
            Graphics,
            new RenderLayer(interior, interiorWidth, height, new PlayfieldLayer(this)),
            new RenderLayer(interior, interiorWidth, height, new ItemLayer(this)),
            new RenderLayer(interior, interiorWidth, height, new EffectsLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new ObjectLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new SidebarLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new RoomLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new SpriteLayer(this)),
            new RenderLayer(hud, hudWidth, height, new HudLayer(this)));
    }

    // $E3F5: a score above the high score becomes it. The scores are packed BCD, most significant byte first.
    private void KeepHighScore()
    {
        ReadOnlySpan<byte> scores = Loop.Scores.Bytes;

        for (int player = 0; player < 2; player++)
        {
            ReadOnlySpan<byte> score = scores.Slice(player * HudModel.ScoreBytes, HudModel.ScoreBytes);

            if (score.SequenceCompareTo(_highScore) > 0)
            {
                score.CopyTo(_highScore);
            }
        }
    }

    // $45A7-$45E1: S, U, P, O and R and the Commodore key, which is Tab here, all down at once.
    private bool SuperKeys() => s_superKeys.All(Keyboard.IsPressed);

    // Fire on either joystick: a port reads with the bit clear.
    private byte TitleFire()
    {
        ReadOnlySpan<byte> ports = _input.Read();

        return (byte)(ports[0] & ports[1]);
    }

    private void PlaySong(Song song)
    {
        if (AudioOptions.MusicOn)
        {
            Sound.Play(song.ToString(), repeat: true);
        }
    }

    // The songs the game has asked for. The level's own tunes go round again; the others play once.
    private void PlaySongs()
    {
        while (Loop.TakeSong() is Song song)
        {
            if (AudioOptions.MusicOn)
            {
                Sound.Play(song.ToString(), song is Song.Theme or Song.Resume or Song.FinalTheme);
            }
        }
    }

    // $1C and $1E are the level's background colours, and the freeze swaps them ($15CF, $2F68).
    private void ShowColours()
    {
        int colours = Loop.Flow.Colours;

        if (colours != _playfield.Colours)
        {
            _playfield = _playfield.WithColours(colours);
            _sidebar = _sidebar with { Colours = colours };
        }
    }

    private SpriteModel Sprites() => new(
        Loop.Entities.State,
        Loop.Entities.X,
        Loop.Entities.Y,
        Loop.Entities.Frame,
        Loop.Entities.SpriteBase,
        Loop.Entities.Colour,
        Loop.Entities.FlashTimer,
        Loop.Entities.SpriteEnable)
    {
        FoodArt = Loop.FoodArt(),
        BonusStage = Loop.Clear.SuperBonus is { Showing: true } ? Loop.Clear.BonusStage : -1,
        Boss = Loop.Boss is { Active: true, Shown: true } boss ? new(boss.X, boss.Y, boss.Frame, boss.Colour) : null,
    };

    private HudModel Hud() => new(
        Loop.Scores.Bytes[..HudModel.ScoreBytes],
        Loop.Scores.Bytes[HudModel.ScoreBytes..],
        _highScore,
        (sbyte)Loop.PlayerTable.Lives[0],
        (sbyte)Loop.PlayerTable.Lives[1],
        (sbyte)Loop.PlayerTable.Credits + 1);

    private ObjectsModel Objects() => new(
        Loop.Pop.Drawn,
        Loop.Pop.DrawnColumn,
        Loop.Pop.DrawnRow,
        Loop.Pop.DrawnSubY,
        _playfield.Colours)
    {
        SlotColours = Loop.Pop.DrawnColour.ToArray(),
    };

    private EffectsModel Effects() => new(
        Loop.Water.Mask != 0,
        Loop.Water.Columns,
        Loop.Water.Rows,
        Loop.Fire.Columns,
        Loop.Fire.Rows,
        Loop.Fire.Lengths,
        Loop.Fire.Characters,
        _playfield.Colours)
    {
        ShowsGiant = Loop.Giant.Shown,
        GiantColumn = Loop.Giant.Column,
        GiantRow = Loop.Giant.Row - GiantBubble.TopRows,
    };

    // A room clears the screen and the render pass that draws the level's items is not run while it lasts.
    private ItemsModel Items() => new(
        Loop.Room.Active ? s_hiddenItems : Loop.Items.Type,
        Loop.Items.Column,
        Loop.Items.Row,
        Loop.Items.Art,
        Loop.Items.Colour,
        _playfield.Colours);

    // A bonus room over the level: the clock and the counts while it lasts, and the results after.
    private RoomModel Room()
    {
        ItemRoom room = Loop.Room;

        if (Loop.Ending.Blank)
        {
            return new([], 0, 0, _playfield.Colours, _playfield.Tile, Loop.Ending.Lines, false);
        }

        if (!room.Active)
        {
            return new([], 0, 0, _playfield.Colours, _playfield.Tile, [], false);
        }

        List<RoomText> lines = [];

        if (room.Phase == ItemRoom.Stage.Playing)
        {
            lines.Add(new(RoomClockColumn, 0, RoomClockColour, $"{Loop.Timer.Seconds:D2}"));
            lines.Add(new(0, RoomCountRow, RoomFirstColour, $"{room.Counts[0]:D2}"));
            lines.Add(new(RoomSecondColumn, RoomCountRow, RoomSecondColour, $"{room.Counts[1]:D2}"));
        }

        lines.AddRange(room.Lines);

        return new(
            [.. room.Items.Where(item => item.X != 0 || item.Y != 0).Select(item => (item.Column, item.Row))],
            room.Art,
            room.Colour,
            _playfield.Colours,
            _playfield.Tile,
            lines,
            room.Phase == ItemRoom.Stage.Results);
    }

    private sealed class PlayfieldLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._playfieldView.Draw(game._playfield);
    }

    private sealed class EffectsLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._effectsView.Draw(game.Effects());
    }

    private sealed class ItemLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._itemView.Draw(game.Items());
    }

    private sealed class RoomLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._roomView.Draw(game.Room());
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
