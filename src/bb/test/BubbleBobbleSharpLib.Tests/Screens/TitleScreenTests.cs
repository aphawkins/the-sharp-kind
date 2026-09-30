// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Renditions.EightBit;
using BubbleBobbleSharpLib.Fakes;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Screens;
using SharpKind.Audio;
using SharpKind.Fakes.Assets;
using SharpKind.Fakes.Audio;
using SharpKind.Fakes.Input;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Screens;

// $4556-$4590: the title screen, and the way the game goes to it and from it.
public sealed class TitleScreenTests
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
    public void OpensOnTheTitleAndFireStartsTheGame()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);

        Assert.Equal(BbScreen.Title, game.Screen);

        game.Update();
        Assert.Equal(BbScreen.Title, game.Screen);

        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.Spacebar, ConsoleModifiers.None);
        game.Update();

        Assert.Equal(BbScreen.Play, game.Screen);
        Assert.Equal(1, game.CurrentLevel);
    }

    [Fact]
    public void TheSecondPlayersFireStartsItToo()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.Q, ConsoleModifiers.None);

        game.Update();

        Assert.Equal(BbScreen.Play, game.Screen);
    }

    // $45A7-$45E1, $45E3-$462A: S, U, P, O, R and the Commodore key held together start the game in super mode, in which the
    // enemies of every level are a class up: level 1's are class 0, which is state 2, and are class 1, state 3.
    [Fact]
    public void SixKeysHeldTogetherStartTheGameWithHarderEnemies()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;

        foreach (ConsoleKey key in new[] { ConsoleKey.S, ConsoleKey.U, ConsoleKey.P, ConsoleKey.O, ConsoleKey.R, ConsoleKey.Tab })
        {
            keyboard.KeyDown(key, ConsoleModifiers.None);
        }

        game.Update();

        Assert.Equal(BbScreen.Play, game.Screen);
        Assert.Contains(Enumerable.Range(2, 6), slot => game.Loop.Entities.State[slot] == 3);
        Assert.DoesNotContain(Enumerable.Range(2, 6), slot => game.Loop.Entities.State[slot] == 2);
    }

    [Fact]
    public void FiveOfTheSixKeysStartNothing()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;

        foreach (ConsoleKey key in new[] { ConsoleKey.S, ConsoleKey.U, ConsoleKey.P, ConsoleKey.O, ConsoleKey.R })
        {
            keyboard.KeyDown(key, ConsoleModifiers.None);
        }

        game.Update();

        Assert.Equal(BbScreen.Title, game.Screen);
    }

    [Fact]
    public void ANewGameStartsFromScratch()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);
        GameLoop before = game.Loop;
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.Spacebar, ConsoleModifiers.None);

        game.Update();

        Assert.NotSame(before, game.Loop);
    }

    [Fact]
    public void ATitleTunePlaysWhenTheTitleIsShown()
    {
        FakeAbstraction abstraction = new();

        _ = Game(abstraction);

        Assert.Equal(1, ((FakeSound)abstraction.Sound).PlayMusicCount);
    }

    [Fact]
    public void ScoreAboveTheHighScoreBecomesIt()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());
        game.Play();
        game.Loop.Scores.Add(2, 0x30);
        game.Loop.Scores.Add(1, 0x12);

        game.Update();

        Assert.Equal([0x00, 0x12, 0x30], game.HighScore.ToArray());
    }

    [Fact]
    public void TheTitleDrawsItsText()
    {
        RecordingGraphics graphics = new(320, 200);
        BubbleBobbleMain game = Game(new FakeAbstraction(graphics, new(320, 200)));

        game.Draw();

        Assert.Contains(graphics.LeftTexts, x => x.Text == "! PRESS FIRE FOR BUBBLE BOBBLE !");
        Assert.Contains(graphics.LeftTexts, x => x.Text == "WRITTEN BY  STEPHEN RUDDY.");
    }

    private static BubbleBobbleMain Game(FakeAbstraction abstraction)
        => new(abstraction, new FakeAssetLocator(), new EightBitRendition(), s_levels, s_zones, new AudioOptions { MusicOn = true });
}
