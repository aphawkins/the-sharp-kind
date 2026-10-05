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
    public void OpensOnTheTitleAndFireGoesOnToTheChoiceOfPlayers()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);

        Assert.Equal(BbScreen.Title, game.Screen);

        game.Update();
        Assert.Equal(BbScreen.Title, game.Screen);

        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.Spacebar, ConsoleModifiers.None);
        game.Update();

        Assert.Equal(BbScreen.Select, game.Screen);
    }

    [Fact]
    public void TheSecondPlayersFireStartsItToo()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = Game(abstraction);
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.Q, ConsoleModifiers.None);

        game.Update();

        Assert.Equal(BbScreen.Select, game.Screen);
    }

    // $45A7-$45E1, $45E3-$462A: S, U, P, O, R and the Commodore key held together go on to the choice of players in super mode, in which the
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
        Assert.Equal(BbScreen.Select, game.Screen);

        keyboard.ClearPressed();
        keyboard.KeyDown(ConsoleKey.D1, ConsoleModifiers.None);
        game.Update();

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
        game.Select();
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.D1, ConsoleModifiers.None);

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

    // $0406-$040B: the top score is 20000, from round 20, until it is beaten.
    [Fact]
    public void TheTopScoreStartsAtTwentyThousandFromRoundTwenty()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());

        Assert.Equal([0x00, 0x20, 0x00], game.HighScore.ToArray());
        Assert.Equal(0x13, game.TopRound);
    }

    // $E3F5: a score above the high score becomes it, and the round its player reached the top round.
    [Fact]
    public void ScoreAboveTheHighScoreBecomesIt()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());
        game.Play();
        game.Loop.PlayerTable.Round[0] = 4;
        game.Loop.Scores.Add(2, 0x30);
        game.Loop.Scores.Add(1, 0x12);
        game.Loop.Scores.Add(0, 0x01);

        game.Update();

        Assert.Equal([0x01, 0x12, 0x30], game.HighScore.ToArray());
        Assert.Equal(4, game.TopRound);
    }

    [Fact]
    public void ScoreBelowTheHighScoreLeavesItAndItsRound()
    {
        BubbleBobbleMain game = Game(new FakeAbstraction());
        game.Play();
        game.Loop.PlayerTable.Round[0] = 4;
        game.Loop.Scores.Add(1, 0x12);

        game.Update();

        Assert.Equal([0x00, 0x20, 0x00], game.HighScore.ToArray());
        Assert.Equal(0x13, game.TopRound);
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
