// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpKind.Abstraction;

namespace EliteSharpLib.Tests;

[Trait("Level", "Integration")]
public class EliteMainTests
{
    [Fact]
    public void ConstructAndUpdateWithFakeAbstractionSucceeds()
    {
        // Arrange: the same DI composition SDLProgram.Main builds
        // (AddEliteConfig + AddEliteMain), with the real IAbstraction
        // swapped for a fake so no SDL window, sound device or keyboard is
        // needed. Everything else, including the real shipped assets, is
        // production wiring.
        string configDirectory = Path.Combine(Path.GetTempPath(), "EliteMainTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceCollection services = new();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<IAbstraction>(_ => new FakeAbstraction());
            services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Graphics);
            services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Layout);
            services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Sound);
            services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Keyboard);
            services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Gamepad);
            services.AddSingleton(_ => TestAssets.Locator());
            services.AddEliteConfig(configDirectory);
            services.AddEliteControls(configDirectory);
            services.AddEliteMain(EliteServiceCollectionExtensions.LoadRendition("16-bit", NullLoggerFactory.Instance));

            using ServiceProvider provider = services.BuildServiceProvider();

            // Act
            EliteMain game = provider.GetRequiredService<EliteMain>();
            game.Update();

            // Assert
            Assert.True(game.IsRunning);
        }
        finally
        {
            if (Directory.Exists(configDirectory))
            {
                Directory.Delete(configDirectory, recursive: true);
            }
        }
    }
}
