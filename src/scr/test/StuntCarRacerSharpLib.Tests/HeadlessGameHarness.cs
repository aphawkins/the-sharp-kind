// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind;
using SharpKind.Assets;
using SharpKind.Audio;
using SharpKind.Fakes.Harness;
using SharpKind.Fakes.Input;
using StuntCarRacerSharpLib.Fakes;
using StuntCarRacerSharpLib.Screens;

namespace StuntCarRacerSharpLib.Tests;

// Drives the real StuntCarRacerMain against a real SoftwareGraphics with no
// SDL window (generalising StuntCarRacerMainTests.StartRace's menu->race
// key sequence into a scripted timeline any test can replay), for tests
// that need several ticks of real gameplay and, occasionally, a rendered
// frame to eyeball.
internal sealed class HeadlessGameHarness : HeadlessGameHarnessBase<GameStateSummary>
{
    // randomSeed replaces the game's unseeded Random.Shared, so a run can be
    // reproduced exactly. Null keeps the shipped behaviour. Frame baselines
    // need it: the opponent's steering jitter and the engine sound's period
    // are both drawn from it, so without a fixed seed no two runs compose the
    // same frame.
    public HeadlessGameHarness(int width = 640, int height = 400, int? randomSeed = null)
        : base(width, height, AssetLocator.Create())
    {
        FakeAbstraction abstraction = new(Graphics, new(width, height));
        Keyboard = (FakeKeyboard)abstraction.Keyboard;
        Game = new(
            abstraction,
            AssetLocator.Create(),
            new AudioOptions(),
            new RandomSource(randomSeed is int seed ? new Random(seed) : Random.Shared));
    }

    public StuntCarRacerMain Game { get; }

    public override GameStateSummary State => new(
        Game.Screens.CurrentId,
        Game.Screens.CurrentId is GameMode.GameInProgress or GameMode.GameOver,
        Game.Race.Car.CurrentPiece,
        Game.Race.Opponent.CurrentPiece,
        Game.Race.Opponent.DistanceToPlayer());

    protected override void UpdateGame() => Game.Update();

    protected override void DrawGame() => Game.Draw();
}
