// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Diagnostics.CodeAnalysis;
using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharp.Renditions.EightBit;
using BubbleBobbleSharpLib.Fakes;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Screens;
using SharpKind;
using SharpKind.Audio;
using SharpKind.Fakes.Assets;
using SharpKind.Fakes.Audio;
using SharpKind.Fakes.Input;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Screens;

// $F005-$F0E3: the choice of players, and $08E4-$091B: the story before the first level.
public sealed class SelectScreenTests
{
    private static readonly string s_assets = Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels");

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(s_assets, "levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(s_assets, "zones.json"));

    [Fact]
    public void WaitsForOneOrTwo()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Selecting(abstraction);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.Spacebar, ConsoleModifiers.None);

        game.Update();

        Assert.Equal(BbScreen.Select, game.Screen);
    }

    [Theory]
    [InlineData(ConsoleKey.D1, 8)]
    [InlineData(ConsoleKey.D2, 7)]
    public void OneOrTwoStartsAGameForThatManyAfterTheStory(ConsoleKey key, int credits)
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Selecting(abstraction);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(key, ConsoleModifiers.None);

        game.Update();

        Assert.Equal(BbScreen.Story, game.Screen);
        Assert.Equal(1, game.CurrentLevel);
        Assert.Equal(credits, game.Loop.PlayerTable.Credits);
    }

    // $0913, $7BC8: 255 frames, measured under VICE from the text at $08F0 to the clear at $091B.
    [Fact]
    public void TheStoryLastsOneHundredAndTwentyEightUpdates()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Selecting(abstraction);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.D1, ConsoleModifiers.None);
        game.Update();

        for (int update = 1; update < StoryScreen.Passes; update++)
        {
            game.Update();
            Assert.Equal(BbScreen.Story, game.Screen);
        }

        game.Update();

        Assert.Equal(BbScreen.Play, game.Screen);
    }

    [Fact]
    public void TheThemeStartsWithTheStory()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Selecting(abstraction);
        int before = ((FakeSound)abstraction.Sound).PlayMusicCount;
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.D1, ConsoleModifiers.None);

        game.Update();

        Assert.Equal(before + 1, ((FakeSound)abstraction.Sound).PlayMusicCount);
    }

    [Fact]
    public void TheChoiceShowsTheScoresRoundsAndTop()
    {
        RecordingGraphics graphics = Graphics();
        BubbleBobbleMain game = Selecting(new FakeAbstraction(graphics, new(320, 200)));
        game.Loop.PlayerTable.Round[1] = 4;

        game.Draw();

        Assert.Contains(graphics.LeftTexts, x => x.Text == "! PRESS 1 OR 2 TO PLAY !");
        Assert.Contains(graphics.LeftTexts, x => x.Text == "01");
        Assert.Contains(graphics.LeftTexts, x => x.Text == "05");
        Assert.Equal(2, graphics.LeftTexts.Count(x => x.Text == "     00"));
        Assert.Contains(graphics.LeftTexts, x => x.Text == "  20000");
        Assert.Contains(graphics.LeftTexts, x => x.Text == "20");
    }

    [Fact]
    public void TheStoryDrawsItsText()
    {
        RecordingGraphics graphics = Graphics();
        FakeAbstraction abstraction = new(graphics, new(320, 200));
        BubbleBobbleMain game = Selecting(abstraction);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.D1, ConsoleModifiers.None);
        game.Update();

        game.Draw();

        Assert.Contains(graphics.LeftTexts, x => x.Text == "NOW, IT IS THE BEGINNING OF A");
        Assert.Contains(graphics.LeftTexts, x => x.Text == "! GOOD  LUCK !");
    }

    // $08B1-$08DA: up to 200 stars, none where a rule or the text, its spaces too, has been written over them.
    [Fact]
    public void TheStarsAreNotUnderTheTextOrTheRules()
    {
        TitleModel screen = new(SelectScreen.Text) { Rules = SelectScreen.Rules };

        TitleModel stars = Starfield.Scatter(new RandomSource(new Random(1)), screen);

        Assert.InRange(stars.Stars.Count, 1, 200);
        Assert.DoesNotContain(stars.Stars, x => screen.Lines.Any(line => Covers(line.Column, line.Row, line.Text.Length, x)));
        Assert.DoesNotContain(stars.Stars, x => screen.Rules.Any(rule => Covers(rule.Column, rule.Row, rule.Length, x)));
        Assert.Contains(stars.Stars, x => x.Big);
        Assert.Contains(stars.Stars, x => !x.Big);
    }

    // $F01A-$F042: the players' dragons, the first sprite, green above blue.
    [Fact]
    public void TheChoiceShowsBothDragons()
    {
        SpriteModel dragons = SelectScreen.Dragons;

        Assert.True(dragons.IsDrawn(0));
        Assert.True(dragons.IsDrawn(1));
        Assert.False(dragons.IsDrawn(2));
        Assert.Equal(0, dragons.Sprite(0));
        Assert.Equal((0x68, 0x65, 5), (dragons.X(0), dragons.Y(0), dragons.Colour(0)));
        Assert.Equal((0x68, 0x7D, 3), (dragons.X(1), dragons.Y(1), dragons.Colour(1)));
    }

    // $08F3-$0906: both dragons in bubbles, whether the second player is in the game or not.
    [Fact]
    public void TheStoryShowsBothDragonsInBubbles()
    {
        SpriteModel dragons = StoryScreen.Dragons(0);

        Assert.Equal(0, dragons.TransitFrame);
        Assert.Equal((0x74, 0x83), (dragons.X(0), dragons.Y(0)));
        Assert.Equal((0xE4, 0x83), (dragons.X(1), dragons.Y(1)));
        Assert.True(dragons.IsDrawn(1));
    }

    // $0746: with the split on, the bubbles turn every eight frames, four passes.
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 1)]
    [InlineData(7, 1)]
    [InlineData(8, 0)]
    public void TheStorysBubblesTurnEveryFourPasses(int passes, int frame)
        => Assert.Equal(frame, StoryScreen.Dragons(passes).TransitFrame);

    // The dragons' sheets, which the views paint from.
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "SetImage takes the bitmap on, and the graphics disposes what it holds.")]
    private static RecordingGraphics Graphics()
    {
        RecordingGraphics graphics = new(320, 200);
        graphics.SetImage("SpritesGame", new FastBitmap(4, 8));
        graphics.SetImage("BubbleDragonInBubble", new FastBitmap(4, 8));

        return graphics;
    }

    private static bool Covers(int column, int row, int length, TitleStar star)
        => star.Row == row && star.Column >= column && star.Column < column + length;

    private static BubbleBobbleMain Selecting(FakeAbstraction abstraction)
    {
        BubbleBobbleMain game = new(
            abstraction, new FakeAssetLocator(), new EightBitRendition(), s_levels, s_zones, new AudioOptions { MusicOn = true });
        game.Select();

        return game;
    }
}
