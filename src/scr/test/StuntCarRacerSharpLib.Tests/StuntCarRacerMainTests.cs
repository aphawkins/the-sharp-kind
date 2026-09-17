// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Assets;
using SharpKind.Audio;
using SharpKind.Fakes.Audio;
using SharpKind.Fakes.Input;
using SharpKind.Graphics.Fakes;
using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Fakes;
using StuntCarRacerSharpLib.Screens;
using StuntCarRacerSharpLib.Tracks;
using Xunit;

namespace StuntCarRacerSharpLib.Tests;

public class StuntCarRacerMainTests
{
    [Fact]
    public void ConstructWithFakeAbstractionSucceeds()
    {
        // Arrange
        FakeAbstraction abstraction = new();

        // Act
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());

        // Assert
        Assert.NotNull(game);
    }

    [Fact]
    public void ConstructWithNullAbstractionThrows()
        => Assert.Throws<ArgumentNullException>(() => new StuntCarRacerMain(null!, AssetLocator.Create()));

    [Fact]
    public void ConstructWithAudioOptionsSucceeds()
    {
        // Arrange
        FakeAbstraction abstraction = new();

        // Act
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create(), new AudioOptions { MusicOn = false, EffectsOn = false });

        // Assert
        Assert.NotNull(game);
    }

    [Fact]
    public void ConstructWithNullAudioOptionsThrows()
        => Assert.Throws<ArgumentNullException>(() => new StuntCarRacerMain(new FakeAbstraction(), AssetLocator.Create(), null!));

    [Fact]
    public void PhysicsStepsEveryFrameGapTicksDuringRace()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        StartRace(game, abstraction);

        // Act
        List<int> movedTicks = [];
        for (int tick = 0; tick < 12; tick++)
        {
            game.Update();
            if (game.Race.FrameMoved)
            {
                movedTicks.Add(tick);
            }
        }

        // Assert: the physics ran on three of the twelve ticks, four apart
        Assert.Equal(3, movedTicks.Count);
        Assert.Equal(4, movedTicks[1] - movedTicks[0]);
        Assert.Equal(4, movedTicks[2] - movedTicks[1]);
    }

    [Fact]
    public void EngineSoundPitchesEveryTickDuringRace()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeSound sound = (FakeSound)abstraction.Sound;
        StartRace(game, abstraction);
        int playsAtRaceStart = sound.PlayLoopCount;

        // Act
        for (int tick = 0; tick < 20; tick++)
        {
            game.Update();
        }

        // Assert: the engine loop is pitched at the full tick rate
        Assert.Equal(20, sound.PlayLoopCount - playsAtRaceStart);
    }

    [Fact]
    public void TrackMenuAndPreviewPlayNoEngineSound()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        FakeSound sound = (FakeSound)abstraction.Sound;

        // Act: a few menu ticks, then select the track to run the preview
        game.Update();
        game.Update();
        PressKey(game, keyboard, ConsoleKey.S);
        for (int tick = 0; tick < 10; tick++)
        {
            game.Update();
        }

        // Assert
        Assert.Equal(0, sound.PlayLoopCount);
    }

    [Fact]
    public void PauseFreezesTheRaceAndSilencesTheEngine()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        FakeSound sound = (FakeSound)abstraction.Sound;
        StartRace(game, abstraction);
        int raceTick = game.Race.RaceTick;
        int playsAtPause = sound.PlayLoopCount;

        // Act: 'P' pauses, then a dozen ticks pass
        PressKey(game, keyboard, ConsoleKey.P);
        for (int tick = 0; tick < 12; tick++)
        {
            game.Update();
        }

        // Assert: nothing the race advances moved, and the engine stayed quiet
        Assert.Equal(raceTick, game.Race.RaceTick);
        Assert.False(game.Race.FrameMoved);
        Assert.Equal(playsAtPause, sound.PlayLoopCount);
    }

    [Fact]
    public void ResumeRestartsTheRaceAfterAPause()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        StartRace(game, abstraction);
        PressKey(game, keyboard, ConsoleKey.P);
        int raceTick = game.Race.RaceTick;

        // Act: 'O' resumes
        PressKey(game, keyboard, ConsoleKey.O);
        for (int tick = 0; tick < 12; tick++)
        {
            game.Update();
        }

        // Assert: the race clock is running again
        Assert.True(game.Race.RaceTick > raceTick);
    }

    [Fact]
    public void FrameGapKeysTuneThePhysicsRate()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        int frameGap = game.Race.FrameGap;

        // Act & Assert: F10 steps the physics less often, F9 more often
        PressKey(game, keyboard, ConsoleKey.F10);
        Assert.Equal(frameGap + 1, game.Race.FrameGap);

        PressKey(game, keyboard, ConsoleKey.F9);
        Assert.Equal(frameGap, game.Race.FrameGap);
    }

    [Fact]
    public void FrameGapDoesNotDropBelowOne()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;

        // Act: press F9 more times than the default gap allows
        for (int press = 0; press < game.Race.FrameGap + 3; press++)
        {
            PressKey(game, keyboard, ConsoleKey.F9);
        }

        // Assert
        Assert.Equal(1, game.Race.FrameGap);
    }

    [Fact]
    public void StatsKeyTogglesTheOverlay()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;

        // Act & Assert: F5 is a toggle, not a two-key pair like the pause
        Assert.False(game.Race.ShowStats);

        PressKey(game, keyboard, ConsoleKey.F5);
        Assert.True(game.Race.ShowStats);

        PressKey(game, keyboard, ConsoleKey.F5);
        Assert.False(game.Race.ShowStats);
    }

    [Fact]
    public void PlayerFreezeStopsTheCarButNotTheRace()
    {
        // Arrange: the car starts in the air above the start piece and falls
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        StartRace(game, abstraction);
        int playerY = game.Race.Car.PlayerY;
        int raceTick = game.Race.RaceTick;

        // Act: F6 freezes the player, then a dozen ticks pass
        PressKey(game, keyboard, ConsoleKey.F6);
        RunTicks(game, 12);

        // Assert: the drop stopped mid-air, but the race clock ran on
        Assert.Equal(playerY, game.Race.Car.PlayerY);
        Assert.True(game.Race.RaceTick > raceTick);
    }

    [Fact]
    public void PlayerKeepsMovingWithoutTheFreeze()
    {
        // Arrange: the control run for the freeze above
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        StartRace(game, abstraction);
        int playerY = game.Race.Car.PlayerY;

        // Act
        RunTicks(game, 12);

        // Assert
        Assert.NotEqual(playerY, game.Race.Car.PlayerY);
    }

    [Fact]
    public void OpponentFreezeStopsTheOpponentOnly()
    {
        // Arrange: the opponent only starts moving once the player has landed
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        StartRace(game, abstraction);
        RunTicks(game, 200);
        int opponentPiece = game.Race.Opponent.CurrentPiece;
        int raceTick = game.Race.RaceTick;

        // Act: F7 freezes the opponent, then a hundred ticks pass
        PressKey(game, keyboard, ConsoleKey.F7);
        RunTicks(game, 100);

        // Assert: the opponent stopped where it was; the race clock did not
        Assert.Equal(opponentPiece, game.Race.Opponent.CurrentPiece);
        Assert.True(game.Race.RaceTick > raceTick);
    }

    [Fact]
    public void OpponentKeepsMovingWithoutTheFreeze()
    {
        // Arrange: the control run for the freeze above
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        StartRace(game, abstraction);
        RunTicks(game, 200);
        int opponentPiece = game.Race.Opponent.CurrentPiece;

        // Act
        RunTicks(game, 100);

        // Assert
        Assert.NotEqual(opponentPiece, game.Race.Opponent.CurrentPiece);
    }

    [Fact]
    public void FreezesClearWhenARaceStarts()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        PressKey(game, keyboard, ConsoleKey.F6);
        PressKey(game, keyboard, ConsoleKey.F7);

        // Act
        StartRace(game, abstraction);

        // Assert
        Assert.False(game.Race.PlayerPaused);
        Assert.False(game.Race.OpponentPaused);
    }

    // Drives the game from the track menu into the race: S selects the
    // track, then S again (read on a preview physics tick) starts the race.
    [Fact]
    public void TurnAroundKeyPointsTheCarTheOppositeWay()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        StartRace(game, abstraction);
        int before = game.Race.Car.YAngle;

        // Act
        PressKey(game, keyboard, ConsoleKey.R);

        // Assert: the car dropping onto the track can steer a little in the
        // same tick, so allow a small drift either side of the half turn.
        int turned = (game.Race.Car.YAngle - before) & (Track.MaxAngle - 1);
        Assert.InRange(turned, AmigaTrig.Degrees180 - 64, AmigaTrig.Degrees180 + 64);
    }

    [Fact]
    public void MenuKeyAbandonsTheRaceForTheTrackMenu()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        FakeSound sound = (FakeSound)abstraction.Sound;
        StartRace(game, abstraction);
        int playsAtRace = sound.PlayLoopCount;
        Assert.Equal(GameMode.GameInProgress, game.Screens.CurrentId);

        // Act
        PressKey(game, keyboard, ConsoleKey.M);
        RunTicks(game, 12);

        // Assert: the menu is current, the opponent is cleared and the
        // engine stays silent
        Assert.Equal(GameMode.TrackMenu, game.Screens.CurrentId);
        Assert.True(game.Race.Opponent.OpponentId < 0);
        Assert.Equal(playsAtRace, sound.PlayLoopCount);
    }

    [Fact]
    public void MenuKeyWorksWhileTheRaceIsPaused()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        StartRace(game, abstraction);
        PressKey(game, keyboard, ConsoleKey.P);

        // Act
        PressKey(game, keyboard, ConsoleKey.M);

        // Assert
        Assert.Equal(GameMode.TrackMenu, game.Screens.CurrentId);
    }

    [Fact]
    public void BackspaceTogglesTheOutsideView()
    {
        // Arrange
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        StartRace(game, abstraction);
        Assert.False(game.Race.OutsideView);

        // Act
        PressKey(game, keyboard, ConsoleKey.Backspace);

        // Assert
        Assert.True(game.Race.OutsideView);

        // Act: and back again
        PressKey(game, keyboard, ConsoleKey.Backspace);

        // Assert
        Assert.False(game.Race.OutsideView);
    }

    // The cockpit is the inside view's; from outside, the car itself is
    // drawn and only the text overlays remain.
    [Fact]
    public void OutsideViewDrawsTheCarInsteadOfTheCockpit()
    {
        // Arrange
        RecordingGraphics graphics = new(640, 400);
        FakeAbstraction abstraction = new(graphics, graphics.Layout);
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;
        StartRace(game, abstraction);

        // The car is only in the scene once physics has stepped at least once, so this waits a full FrameGap rather than the very first tick.
        RunTicks(game, 4);

        game.Draw();
        int insidePolygons = graphics.FilledPolygons.Count;
        Assert.NotEmpty(graphics.ImageParts);

        // Act
        PressKey(game, keyboard, ConsoleKey.Backspace);
        graphics.Clear();
        graphics.ImageParts.Clear();
        graphics.FilledPolygons.Clear();
        game.Draw();

        // Assert: no cockpit art, and more world polygons than before - the
        // player's own car is in the scene now
        Assert.Empty(graphics.ImageParts);
        Assert.True(graphics.FilledPolygons.Count > insidePolygons);
    }

    private static void StartRace(StuntCarRacerMain game, FakeAbstraction abstraction)
    {
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;

        // menu -> preview -> race. The preview now reads keys every tick, so one press does here what it does on every other screen.
        PressKey(game, keyboard, ConsoleKey.S);
        PressKey(game, keyboard, ConsoleKey.S);
    }

    private static void RunTicks(StuntCarRacerMain game, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            game.Update();
        }
    }

    private static void PressKey(StuntCarRacerMain game, FakeKeyboard keyboard, ConsoleKey key)
    {
        keyboard.KeyDown(key, ConsoleModifiers.None);
        game.Update();
        keyboard.KeyUp(key, ConsoleModifiers.None);
    }
}
