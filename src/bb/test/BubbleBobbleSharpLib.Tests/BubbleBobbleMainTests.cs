// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using BubbleBobbleSharp.Renditions.EightBit;
using BubbleBobbleSharpLib.Fakes;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Screens;
using SharpKind.Fakes.Assets;
using SharpKind.Fakes.Input;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests;

public sealed class BubbleBobbleMainTests
{
    // Twelve two-row blocks down each edge plus the last row's top half, both edges.
    private const int SidebarCharacters = 100;

    // The sheets the views draw from. The game repaints them - in the level's colours for the
    // playfield's two, in the sprite's own colour for the players' - before it draws, so the surface
    // has to be holding them by the time a frame is composed.
    private static readonly string[] s_sheets =
        ["LevelTiles", "TileEdges", "Sidebars", "SpritesGame", "ObjectSprites", "ItemChars", "EffectChars"];

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "zones.json"));

    [Fact]
    public void NewGameIsRunning()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());

        Assert.True(game.IsRunning);
    }

    [Fact]
    public void EscapeStopsTheGame()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.Escape, ConsoleModifiers.None);

        game.Update();

        Assert.False(game.IsRunning);
    }

    // $0A64, $0A99: with both players out the game holds for $96 frames, seventy-five updates, and then
    // goes back to the choice of players ($F0E8).
    [Fact]
    public void ReturnsToTheChoiceOfPlayersSeventyFiveUpdatesAfterBothPlayersAreOut()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());
        game.Loop.Entities.State[0] = 0;

        for (int update = 1; update < 75; update++)
        {
            game.Update();
            Assert.Equal(BbScreen.Play, game.Screen);
        }

        game.Update();

        Assert.Equal(BbScreen.Select, game.Screen);
        Assert.True(game.IsRunning);
    }

    // $0A6A-$0A96: a cleared level whose clock has run out scrolls on to the next ($37C9, a row a frame, two an update),
    // with the players it had and the level they reached kept.
    [Fact]
    public void AClearedLevelWhoseTimeIsUpGoesOnToTheNext()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());
        game.Loop.Timer.Seconds = 0;
        game.Loop.Timer.Hurry = 0xFF;
        game.Loop.PlayerTable.Lives[0] = 2;

        game.Update();

        for (int update = 1; update <= LevelScroll.Steps / 2; update++)
        {
            Assert.Equal(1, game.CurrentLevel);
            game.Update();
        }

        Assert.Equal(2, game.CurrentLevel);
        Assert.Equal(1, game.Loop.PlayerTable.Round[0]);
        Assert.Equal(0, game.Loop.PlayerTable.Round[1]);
        Assert.Equal(2, game.Loop.PlayerTable.Lives[0]);
        Assert.Equal(1, game.Loop.Entities.State[0]);
        Assert.Equal(0, game.Loop.Entities.State[1]);
        Assert.False(game.Loop.IsComplete);
    }

    // $37C9: the old level's items, fire and water are characters on the screen, so they go up with it: two rows an update.
    [Fact]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "SetImage takes the bitmap on, and the graphics disposes what it holds.")]
    public void TheOldLevelsItemsFireAndWaterGoUpWithTheScroll()
    {
        RecordingGraphics graphics = new(320, 200);

        foreach (string sheet in s_sheets.Append("BubbleDragonInBubble"))
        {
            graphics.SetImage(sheet, new FastBitmap(4, 8));
        }

        BubbleBobbleMain game = Game(new FakeAbstraction(graphics, new(320, 200)));
        game.Loop.Timer.Seconds = 0;
        game.Loop.Timer.Hurry = 0xFF;

        game.Update();
        game.Update();
        game.Draw();

        Assert.Equal([new(0, -16), Vector2.Zero, new(0, -16), Vector2.Zero], graphics.Translations);
    }

    // $7FB2-$7FEE: an item that skips levels flashes the screen for 30 updates, and scrolls through every one of them, 16
    // updates each, to the level it skips to.
    [Fact]
    public void ASkipScrollsThroughEveryLevelOnTheWay()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());
        game.Loop.World.SkipTo = 3;

        game.Update();

        for (int update = 1; update <= 30 + (3 * (LevelScroll.Steps / 2)); update++)
        {
            Assert.Equal(1, game.CurrentLevel);
            game.Update();
        }

        Assert.Equal(4, game.CurrentLevel);
    }

    // $0A8B: level 100's end is the ending.
    [Fact]
    public void TheLastLevelEndsInTheEnding()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());
        game.ShowLevel(LevelStore.Count);
        game.Loop.Timer.Seconds = 0;
        game.Loop.Timer.Hurry = 0xFF;

        game.Update();

        Assert.True(game.Loop.Ending.Active);
    }

    // The game opens on the title, and the level under it is the first.
    [Fact]
    public void OpensOnTheTitleOverTheFirstLevel()
    {
        BubbleBobbleMain game = new(new FakeAbstraction(), new FakeAssetLocator(), new EightBitRendition(), s_levels, s_zones);

        Assert.Equal(BbScreen.Title, game.Screen);
        Assert.Equal(1, game.CurrentLevel);
    }

    // The flash before a skip under the whole screen, the level and its items trimmed to the 28 columns the decoration leaves, the bubbles and the
    // decoration over the whole 32, a bonus room over them, the players over that, and the HUD in the eight columns past them.
    // All are the full height of the screen, after the whole screen the banner is drawn across. The players share the decoration's band because a sprite
    // may be anywhere on the level, including over its outermost columns.
    [Fact]
    public void ComposesTheFlashLevelItsItemsEffectsBubblesDecorationRoomPlayersAndHudAsNineLayers()
    {
        RecordingGraphics graphics = Draw();

        Assert.Equal(
            [(new Vector2(0, 0), 320f, 200f),
             (new Vector2(0, 0), 320f, 200f),
             (new Vector2(16, 0), 224f, 200f),
             (new Vector2(16, 0), 224f, 200f),
             (new Vector2(16, 0), 224f, 200f),
             (new Vector2(0, 0), 256f, 200f),
             (new Vector2(0, 0), 256f, 200f),
             (new Vector2(0, 0), 256f, 200f),
             (new Vector2(0, 0), 256f, 200f),
             (new Vector2(256, 0), 64f, 200f)],
            graphics.ClipRegions);
    }

    // The HUD opens on what $0969 and $0956 leave: both scores and the high score cleared, and three
    // lives each. A cleared score is still drawn, as a single zero.
    [Fact]
    public void OpensWithClearedScoresAndThreeLivesEach()
    {
        RecordingGraphics graphics = Draw();

        Assert.Equal(2, graphics.LeftTexts.Count(x => x.Text == "     00"));
        Assert.Contains(graphics.LeftTexts, x => x.Text == "  20000");
        Assert.Equal(2, graphics.LeftTexts.Count(x => x.Text == "___"));
    }

    // The level is drawn first and the decoration over it, which is the order the reference works in:
    // setup_level_screen writes the level, and draw_border then writes over its outermost columns.
    //
    // Which sheet each comes from cannot tell them apart here: level 1 has no sidebar design, so its
    // decoration repeats its header tile out of the same tile sheet the level is drawn from. Where
    // they land does - the decoration is only ever at the level's outermost two columns each side.
    [Fact]
    public void DrawsTheLevelBeforeItsDecoration()
    {
        RecordingGraphics graphics = Draw();

        float[] edges = [0f, 8f, 240f, 248f];

        // The hardware sprites - the player and the level's enemies - are drawn after both of them,
        // so they come off the end first.
        int sprites = graphics.ImageParts.Reverse().TakeWhile(x => x.ImageType.StartsWith("SpritesGame", StringComparison.Ordinal)).Count();
        (string ImageType, Vector2 Position, Vector2 Size, Vector2 SourcePosition, Vector2 SourceSize)[] characters =
            [.. graphics.ImageParts.SkipLast(sprites)];

        Assert.Equal(
            edges,
            characters.TakeLast(SidebarCharacters).Select(x => x.Position.X).Distinct().Order());

        // And the level itself was drawn before them, inside those edges.
        Assert.Contains(
            characters.SkipLast(SidebarCharacters),
            x => !edges.Contains(x.Position.X));
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "SetImage takes the bitmap on, and the graphics disposes what it holds.")]
    private static RecordingGraphics Draw()
    {
        RecordingGraphics graphics = new(320, 200);

        foreach (string sheet in s_sheets)
        {
            graphics.SetImage(sheet, new FastBitmap(4, 8));
        }

        Game(new FakeAbstraction(graphics, new(320, 200))).Draw();

        return graphics;
    }

    private static BubbleBobbleMain Game(FakeAbstraction abstraction)
    {
        BubbleBobbleMain game = new(abstraction, new FakeAssetLocator(), new EightBitRendition(), s_levels, s_zones);

        game.Play();

        return game;
    }
}
