// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Fakes;
using EliteSharpLib.Renditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpKind.Abstraction;
using SharpKind.Fakes.Harness;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests;

// Drives the real EliteMain against a real SoftwareGraphics with no SDL window. EliteMain.Run is
// unusable headlessly as-is - it hands off to GameHost.Run's wall-clock loop - so this calls
// Update()/Draw() directly per tick (via HeadlessGameHarnessBase).
internal sealed class HeadlessGameHarness : HeadlessGameHarnessBase<GameStateSummary>
{
    // Loaded once and shared: a rendition holds no state, and loading it per harness reloads its assembly for every test.
    private static readonly InstalledRenditions s_renditions =
        EliteServiceCollectionExtensions.LoadRendition("16-bit", NullLoggerFactory.Instance);

    private readonly ServiceProvider _provider;
    private readonly string _configDirectory;

    // Screen size defaults to the rendition's own. A caller may still name a size; 0x0 is the one
    // thing it must not, since negative ranges blow up star generation.
    // randomSeed replaces the app's unseeded Random.Shared so a run can be reproduced exactly.
    // Golden traces need it: without a fixed seed, laser aim jitter, encounter rolls and ship spins all differ run to run.
    public HeadlessGameHarness(
        int? width = null,
        int? height = null,
        int? randomSeed = null,
        float updatesPerSecond = GameClock.StepsPerSecond,
        int? renderSeed = null)
        : base(
            width ?? s_renditions.Chosen.ScreenWidth,
            height ?? s_renditions.Chosen.ScreenHeight,
            TestAssets.Locator())
    {
        FakeAbstraction abstraction = new(
            Graphics,
            new(width ?? s_renditions.Chosen.ScreenWidth, height ?? s_renditions.Chosen.ScreenHeight));
        Keyboard = (FakeKeyboard)abstraction.Keyboard;

        _configDirectory = Path.Combine(Path.GetTempPath(), "EliteHeadlessHarness_" + Guid.NewGuid().ToString("N"));

        ServiceCollection services = new();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<IAbstraction>(abstraction);
        services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Graphics);
        services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Layout);
        services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Sound);
        services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Keyboard);
        services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Gamepad);
        services.AddSingleton(_ => TestAssets.Locator());
        services.AddEliteConfig(_configDirectory);
        services.AddEliteControls(_configDirectory);
        services.AddEliteMain(s_renditions);

        // After AddEliteMain, so this wins: the container resolves the last
        // registration for a service type, and AddEliteCore registered
        // Random.Shared.
        if (randomSeed is int seed)
        {
            services.AddSingleton(new Random(seed));
        }

        // Left alone by default so the trace reproducibility test can catch drawing that reaches
        // the simulation again. A caller comparing pixels needs it fixed, since the starfield draws from it.
        if (renderSeed is int render)
        {
            services.AddSingleton(new RenderRandom(new Random(render)));
        }

        _provider = services.BuildServiceProvider();
        Game = _provider.GetRequiredService<EliteMain>();

        // Pinned to the game's own rate by default so a step is a tick and golden baselines keep their meaning.
        Game.State.Config.Engine.Graphics.Fps = updatesPerSecond;
    }

    public EliteMain Game { get; }

    public override GameStateSummary State => new(
        Game.State.CurrentScreen,
        Game.State.IsDocked,
        Game.State.IsGameOver);

    // For asserting on parts of the real composition that EliteMain does not
    // reach through - the mission registry, until the missions are wired in.
    public T Resolve<T>()
        where T : notnull
        => _provider.GetRequiredService<T>();

    protected override void UpdateGame() => Game.Update();

    protected override void DrawGame() => Game.Draw();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _provider.Dispose();
            if (Directory.Exists(_configDirectory))
            {
                Directory.Delete(_configDirectory, recursive: true);
            }
        }

        base.Dispose(disposing);
    }
}
