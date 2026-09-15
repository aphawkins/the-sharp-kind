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

// Drives the real EliteMain (the same DI composition SDLProgram.Main
// builds, AddEliteConfig + AddEliteMain) against a real SoftwareGraphics
// with no SDL window, for tests that need several ticks of real gameplay
// and, occasionally, a rendered frame to eyeball. EliteMain.Run is unusable
// headlessly as-is - it hands off to GameHost.Run's real-time,
// wall-clock-waiting loop - so this calls Update()/Draw() directly per
// tick instead (via HeadlessGameHarnessBase).
internal sealed class HeadlessGameHarness : HeadlessGameHarnessBase<GameStateSummary>
{
    // The rendition this harness draws with, loaded once and shared: a
    // rendition holds no state, and loading it per harness reloads its
    // assembly for every test that builds one.
    private static readonly InstalledRenditions s_renditions =
        EliteServiceCollectionExtensions.LoadRendition("16-bit", NullLoggerFactory.Instance);

    private readonly ServiceProvider _provider;
    private readonly string _configDirectory;

    // The screen size defaults to the rendition's own. It was a hardcoded
    // 512x512 until 2026-09-15, which had been wrong since the tier widened
    // to 640 on 2026-07-30: the HUD art is 640 across, so every headless
    // frame lost the right-hand dial cluster and the right edge of the
    // canopy - a frame no commander ever sees, being signed by the golden
    // baselines. A caller may still name a size; 0x0 is the one thing it
    // must not, since negative ranges blow up star generation.
    // randomSeed replaces the app's unseeded Random.Shared, so a run can be
    // reproduced exactly. Null keeps the shipped behaviour. Golden traces
    // need it: without a fixed seed the laser aim jitter, the encounter
    // rolls and the ship spins all differ run to run.
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

        // The drawing's stream is left alone by default, which is what makes
        // the trace reproducibility test mean something: if drawing could
        // ever reach the simulation again, an unseeded render stream would
        // make the traces differ run to run rather than let the coupling
        // pass unnoticed. A caller that compares *pixels* needs it fixed,
        // because the starfield is scattered from it.
        if (renderSeed is int render)
        {
            services.AddSingleton(new RenderRandom(new Random(render)));
        }

        _provider = services.BuildServiceProvider();
        Game = _provider.GetRequiredService<EliteMain>();

        // Every Step is one update, and how much game time an update is
        // worth is now the Fps setting. Pinned to the game's own rate by
        // default so a step is a tick and the golden baselines keep meaning
        // what they meant; a test that wants to prove the game plays the
        // same at some other rate says so.
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
