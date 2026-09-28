// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using BubbleBobbleSharp.Renditions.EightBit;
using BubbleBobbleSharpLib.Fakes;
using BubbleBobbleSharpLib.Levels;
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
    private static readonly string[] s_sheets = ["LevelTiles", "TileEdges", "Sidebars", "SpritesGame", "ObjectSprites", "ItemChars"];

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
    // stops, since there is no front end (step 9) to go back to.
    [Fact]
    public void StopsSeventyFiveUpdatesAfterBothPlayersAreOut()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());
        game.Loop.Entities.State[0] = 0;

        for (int update = 1; update < 75; update++)
        {
            game.Update();
            Assert.True(game.IsRunning);
        }

        game.Update();

        Assert.False(game.IsRunning);
    }

    // $0A6A-$0A96: a cleared level whose clock has run out goes on to the next, with the players it had and the
    // level they reached kept.
    [Fact]
    public void AClearedLevelWhoseTimeIsUpGoesOnToTheNext()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());
        game.Loop.Timer.Seconds = 0;
        game.Loop.Timer.Hurry = 0xFF;
        game.Loop.PlayerTable.Lives[0] = 2;

        game.Update();

        Assert.Equal(2, game.CurrentLevel);
        Assert.Equal(1, game.Loop.PlayerTable.Round[0]);
        Assert.Equal(0, game.Loop.PlayerTable.Round[1]);
        Assert.Equal(2, game.Loop.PlayerTable.Lives[0]);
        Assert.Equal(1, game.Loop.Entities.State[0]);
        Assert.Equal(0, game.Loop.Entities.State[1]);
        Assert.False(game.Loop.IsComplete);
    }

    // $0A8B: level 100's end is the ending, which is not translated.
    [Fact]
    public void TheLastLevelEndsInTheEndingWhichIsNotTranslated()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());
        game.ShowLevel(LevelStore.Count);
        game.Loop.Timer.Seconds = 0;
        game.Loop.Timer.Hurry = 0xFF;

        Assert.Throws<NotSupportedException>(game.Update);
    }

    // There is no front end yet, so the game opens on the first level rather than being asked for one.
    [Fact]
    public void OpensOnTheFirstLevel()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());

        Assert.Equal(1, game.CurrentLevel);
    }

    // There is no progression until Phase 7, so N is the only way to see a level past the first.
    [Fact]
    public void NStepsToTheNextLevel()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.N, ConsoleModifiers.None);

        game.Update();

        Assert.Equal(2, game.CurrentLevel);
    }

    // The hundredth level's neighbour is the first, because there is no level 101 to ask for.
    [Fact]
    public void NWrapsFromTheLastLevelToTheFirst()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);
        game.ShowLevel(LevelStore.Count);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.N, ConsoleModifiers.None);

        game.Update();

        Assert.Equal(1, game.CurrentLevel);
    }

    // The step is one level per press, not one per tick the key is held down.
    [Fact]
    public void HoldingNStepsOneLevelOnly()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.N, ConsoleModifiers.None);

        game.Update();
        game.Update();

        Assert.Equal(2, game.CurrentLevel);
    }

    // The level and its items trimmed to the 28 columns the decoration leaves, the bubbles and the
    // decoration over the whole 32, the players over that, and the HUD in the eight columns past them.
    // All are the full height of the screen. The players share the decoration's band because a sprite
    // may be anywhere on the level, including over its outermost columns.
    [Fact]
    public void ComposesTheLevelItsItemsBubblesDecorationPlayersAndHudAsSixLayers()
    {
        RecordingGraphics graphics = Draw();

        Assert.Equal(
            [(new Vector2(16, 0), 224f, 200f),
             (new Vector2(16, 0), 224f, 200f),
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

        Assert.Equal(3, graphics.LeftTexts.Count(x => x.Text == "     0"));
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
        => new(abstraction, new FakeAssetLocator(), new EightBitRendition(), s_levels, s_zones);
}
