// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Assets;
using SharpKind.Fakes.Input;
using StuntCarRacerSharpLib.Fakes;
using StuntCarRacerSharpLib.Screens;
using Xunit;

namespace StuntCarRacerSharpLib.Tests.Screens;

// Regression: keys used to be read only on physics ticks (one in four), so a brief tap that worked on every other screen did nothing here.
public class TrackPreviewScreenTests
{
    [Fact]
    public void ASinglePressStartsTheRaceWhateverTickItLandsOn()
    {
        // Every offset within one physics cycle, so the press lands on an idle
        // tick as well as on a busy one.
        for (int offset = 0; offset < 4; offset++)
        {
            FakeAbstraction abstraction = new();
            StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
            FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;

            Press(game, keyboard, ConsoleKey.S);
            Assert.Equal(GameMode.TrackPreview, game.Screens.CurrentId);

            for (int tick = 0; tick < offset; tick++)
            {
                game.Update();
            }

            Press(game, keyboard, ConsoleKey.S);

            Assert.Equal(GameMode.GameInProgress, game.Screens.CurrentId);
        }
    }

    [Fact]
    public void ASinglePressGoesBackToTheMenuWhateverTickItLandsOn()
    {
        for (int offset = 0; offset < 4; offset++)
        {
            FakeAbstraction abstraction = new();
            StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
            FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;

            Press(game, keyboard, ConsoleKey.S);

            for (int tick = 0; tick < offset; tick++)
            {
                game.Update();
            }

            Press(game, keyboard, ConsoleKey.M);

            Assert.Equal(GameMode.TrackMenu, game.Screens.CurrentId);
        }
    }

    // Reading the keys first must not have changed how often the physics runs.
    [Fact]
    public void ThePhysicsStillStepsEveryFourthTick()
    {
        FakeAbstraction abstraction = new();
        StuntCarRacerMain game = new(abstraction, AssetLocator.Create());
        FakeKeyboard keyboard = (FakeKeyboard)abstraction.Keyboard;

        Press(game, keyboard, ConsoleKey.S);
        Assert.Equal(GameMode.TrackPreview, game.Screens.CurrentId);

        List<int> movedTicks = [];
        for (int tick = 0; tick < 12; tick++)
        {
            game.Race.FrameMoved = false;
            game.Update();
            if (game.Race.FrameMoved)
            {
                movedTicks.Add(tick);
            }
        }

        Assert.Equal(3, movedTicks.Count);
        Assert.Equal(4, movedTicks[1] - movedTicks[0]);
        Assert.Equal(4, movedTicks[2] - movedTicks[1]);
    }

    private static void Press(StuntCarRacerMain game, FakeKeyboard keyboard, ConsoleKey key)
    {
        keyboard.KeyDown(key, ConsoleModifiers.None);
        game.Update();
        keyboard.KeyUp(key, ConsoleModifiers.None);
    }
}
