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

    // $0406-$0408, $040B: the top score, 20000 until it is beaten, and the round its player had reached, 20.
    private readonly byte[] _highScore = [0x00, 0x20, 0x00];

    private LevelStore _levels;
    private PlayfieldModel _playfield;
    private bool _superRoomShown;
    private SidebarModel _sidebar;
    private ItemsModel? _shownItems;
    private EffectsModel? _shownEffects;
    private LevelChange _change;

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

        _change = new(_levels);
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

    internal byte TopRound { get; private set; } = 0x13;

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

    // $45A1: fire on the title goes on to the choice of players.
    internal void Select() => Select(superMode: false);

    // $45E3-$462A: the cheat makes every level's enemies harder, and the data stays so.
    internal void Select(bool superMode)
    {
        if (superMode)
        {
            _levels = _levels.Super();
        }

        _screens.Set(BbScreen.Select);
    }

    // $F0E3, $08E4: a new game for one or two, from the first level, after the story. The title's tune stops when the
    // game's theme starts.
    internal void StartGame(int players)
    {
        Loop = new GameLoop(new BbRandom(new RandomSource(_random)), players);
        _change = new(_levels);
        ShowLevel(FirstLevel);
        _screens.Set(BbScreen.Story);
    }

    // Goes to the game with the level it has, which is how a test sets one up.
    internal void Play() => _screens.Set(BbScreen.Play);

    [MemberNotNull(nameof(_playfield), nameof(_sidebar))]
    internal void ShowLevel(int number, bool next = false)
    {
        Level level = _levels.Level(number);

        _playfield = Playfield.Build(level);
        _superRoomShown = false;
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

        // $37C9, $7FB2: nothing moves while the screen flashes and scrolls, and the next level starts when it has ($09DC).
        if (_change.Active)
        {
            if (_change.Pass(_playfield, _sidebar, CurrentLevel, Loop.Entities) is int level)
            {
                ShowLevel(level, next: true);
            }

            return;
        }

        Loop.Pass(_input.Read());
        KeepHighScore();

        // $0A6A: a cleared level whose clock has run out scrolls on to the next.
        if (Loop.IsComplete && Loop.EndLevel() is int next)
        {
            _change.ScrollTo(_playfield, _sidebar, CurrentLevel, next);
            PlaySongs();

            return;
        }

        // $7FB2: an item has skipped levels: the screen flashes, and scrolls through every one of them to it ($7FD4).
        if (Loop.SkipLevel() is int skipped)
        {
            _change.SkipTo(skipped);
            PlaySongs();

            return;
        }

        ShowSuperRoom();
        ShowColours();
        ShowWipe();
        PlaySongs();

        // $0AA3, $F0E8: with both players out and the wait over the game goes back to the choice of players.
        if (Loop.Finished)
        {
            _screens.Set(BbScreen.Select);
        }
    }

    private ScreenManager<BbScreen, IGameScreen> CreateScreens(IView<TitleModel> titleView, LayerRunner layers, LayerRunner endingLayers)
    {
        ScreenManager<BbScreen, IGameScreen> screens = new(Keyboard);

        screens.Add(BbScreen.Title, new TitleScreen(TitleFire, SuperKeys, Select, PlaySong, titleView.Draw));
        screens.Add(
            BbScreen.Select,
            new SelectScreen(
                new RandomSource(_random),
                () => Keyboard.IsPressed(ConsoleKey.D1),
                () => Keyboard.IsPressed(ConsoleKey.D2),
                StartGame,
                () => SelectScreen.Build(Hud(), Loop.PlayerTable.Round, TopRound),
                titleView.Draw,
                _spriteView.Draw));
        screens.Add(BbScreen.Story, new StoryScreen(new RandomSource(_random), PlaySongs, Play, titleView.Draw, _spriteView.Draw));
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

        // How far the old level has gone up the screen as it scrolls to the next.
        Vector2 Scrolled() => new(0, -_change.Rows * surface.Layout.CellHeight);

        return new LayerRunner(
            Graphics,
            new RenderLayer(whole, surface.Layout.ScreenWidth, height, new FlashLayer(this, surface)),
            new RenderLayer(interior, interiorWidth, height, new PlayfieldLayer(this)),
            new RenderLayer(interior, interiorWidth, height, new ItemLayer(this))
            {
                // $37C9: the level's items are characters on the screen, and go up with it.
                Offset = Scrolled,
            },
            new RenderLayer(interior, interiorWidth, height, new EffectsLayer(this))
            {
                // So are its fire and water, which stand still while it does.
                Offset = Scrolled,
            },
            new RenderLayer(interior, interiorWidth, height, new WipeLayer(this, surface)),
            new RenderLayer(whole, wholeWidth, height, new ObjectLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new SidebarLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new RoomLayer(this)),
            new RenderLayer(whole, wholeWidth, height, new SpriteLayer(this)),
            new RenderLayer(hud, hudWidth, height, new HudLayer(this)));
    }

    // $E3F5: a score above the high score becomes it, and its player's round ($0409) the top round. The scores are packed
    // BCD, most significant byte first.
    private void KeepHighScore()
    {
        ReadOnlySpan<byte> scores = Loop.Scores.Bytes;

        for (int player = 0; player < 2; player++)
        {
            ReadOnlySpan<byte> score = scores.Slice(player * HudModel.ScoreBytes, HudModel.ScoreBytes);

            if (score.SequenceCompareTo(_highScore) > 0)
            {
                score.CopyTo(_highScore);
                TopRound = Loop.PlayerTable.Round[player];
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

    // `$3621`: the super bonus room is drawn in place of the level, until the level ends.
    private void ShowSuperRoom()
    {
        if (Loop.SuperRoom.Active && !_superRoomShown)
        {
            _superRoomShown = true;
            _playfield = Playfield.Build(SuperBonusRoom.Map(CurrentLevel));
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

    // $2E15: the special item of type 8 wipes the level a row at a time, and it stays so until the next.
    private void ShowWipe()
    {
        int wiped = Loop.WipedRows;

        if (wiped != _playfield.WipedRows)
        {
            _playfield = _playfield.Wiped(wiped);
        }
    }

    // While the wipe goes up the screen the game draws nothing, and the items and the effects stay as they were drawn until
    // a row of blocks covers them.
    private ItemsModel ShownItems()
    {
        if (!Loop.Wiping || _shownItems is null)
        {
            _shownItems = Items();
        }

        return _shownItems;
    }

    private EffectsModel ShownEffects()
    {
        if (!Loop.Wiping || _shownEffects is null)
        {
            _shownEffects = Effects();
        }

        return _shownEffects;
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
        Pictures = Loop.Show.On ? Loop.Show.Pictures.ToArray() : [],
        Expanded = Loop.Entities.SpriteExpand,
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
        Loop.Room.Active || Loop.SuperRoom.Active ? s_hiddenItems : Loop.Items.Type,
        Loop.Items.Column,
        Loop.Items.Row,
        Loop.Items.Art,
        Loop.Items.Colour,
        _playfield.Colours);

    // $7FB2-$7FD1: before a skip the border and background flash red ($00 EOR $02), under everything on the screen; and so
    // do they in the colours a routine that waits puts in them ($1AC0, $2F2D).
    private sealed class FlashLayer(BubbleBobbleMain game, BbViewSurface surface) : ILayerDrawer
    {
        private const int Red = 2;

        private static readonly string[] s_colours =
            ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"];

        public void Draw()
        {
            int? colour = game._change.FlashShows ? Red : game.Loop.Border;

            if (colour is int shown)
            {
                surface.Graphics.DrawRectangleFilled(
                    surface.Layout.Screen.Cell(0, 0),
                    surface.Layout.ScreenWidth,
                    surface.Layout.ScreenRows * surface.Layout.CellHeight,
                    surface.Palette[s_colours[shown]]);
            }
        }
    }

    // $2E2E: while the wipe goes up the screen, its blocks are characters that take the place of the items and the effects.
    private sealed class WipeLayer(BubbleBobbleMain game, BbViewSurface surface) : ILayerDrawer
    {
        // A multicolour block is all 11: the colour RAM's low three bits.
        private const string BlockColour = "3";

        private readonly FastColor _block = surface.Palette[BlockColour];

        public void Draw()
        {
            if (!game.Loop.Wiping)
            {
                return;
            }

            for (int row = 0; row < PlayfieldModel.Rows; row++)
            {
                for (int column = 0; column < PlayfieldModel.Columns; column++)
                {
                    if (game._playfield.Character(column, row) == PlayfieldModel.Block)
                    {
                        surface.Graphics.DrawRectangleFilled(
                            surface.Layout.Playfield.Cell(column, row),
                            surface.Layout.CellWidth,
                            surface.Layout.CellHeight,
                            _block);
                    }
                }
            }
        }
    }

    private sealed class PlayfieldLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw()
        {
            foreach (PlayfieldModel field in game._change.Fields(game._playfield))
            {
                game._playfieldView.Draw(field);
            }
        }
    }

    private sealed class EffectsLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw()
        {
            if (!game._change.Gliding)
            {
                game._effectsView.Draw(game.ShownEffects());
            }
        }
    }

    private sealed class ItemLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw()
        {
            if (!game._change.Gliding)
            {
                game._itemView.Draw(game.ShownItems());
            }
        }
    }

    private sealed class RoomLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw()
        {
            if (!game._change.InTransit)
            {
                game._roomView.Draw(RoomPicture.Build(game.Loop, game._playfield));
            }
        }
    }

    private sealed class ObjectLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw()
        {
            if (!game._change.InTransit)
            {
                game._objectView.Draw(game.Objects());
            }
        }
    }

    private sealed class SidebarLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw()
        {
            // $3C01-$3CB1, $1694: the ROUND and READY!! text is over the level while its seconds ($A9B1) last.
            bool ready = !game._change.Active && game.Loop.Timer.Ready != LevelTimer.Stopped;

            // $09EE, $E9AE: the EXTEND letters are drawn on the level once it is on the screen, and again as they change.
            // Under VICE they stay up through the level's end, are gone while the screen scrolls, and are back on the
            // new level while the players are still gliding in their bubbles.
            ReadOnlySpan<byte> letters = game.Loop.PlayerTable.ExtendLetters;

            foreach (SidebarModel sidebar in game._change.Sidebars(game._sidebar))
            {
                SidebarModel shown = game._change.Scroll is not null
                    ? sidebar
                    : sidebar with { PlayerOneLetters = letters[0], PlayerTwoLetters = letters[1] };

                game._sidebarView.Draw(ready ? shown with { Round = game.CurrentLevel } : shown);
            }
        }
    }

    private sealed class SpriteLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw()
            => game._spriteView.Draw(
                game._change.Sprites(game.Loop.Entities) ?? game.Sprites());
    }

    private sealed class HudLayer(BubbleBobbleMain game) : ILayerDrawer
    {
        public void Draw() => game._hudView.Draw(game.Hud());
    }
}
