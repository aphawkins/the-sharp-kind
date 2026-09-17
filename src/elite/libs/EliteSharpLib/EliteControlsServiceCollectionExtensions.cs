// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpKind.Abstraction.Controls;
using SharpKind.Config;
using SharpKind.Input;

namespace EliteSharpLib;

// Own class because EliteServiceCollectionExtensions is already at CA1506's coupling limit.
public static class EliteControlsServiceCollectionExtensions
{
    // Own file, not inside elite.sharp: a long list a commander edits by hand,
    // kept apart so a mistake in one cannot cost the other.
    private const string ControlsFileName = "elite.controls.sharp";

    /// <summary>
    /// Registers the bindings, written out in full on first run so the file
    /// is there to be edited, and read as the whole answer after that.
    /// </summary>
    public static IServiceCollection AddEliteControls(this IServiceCollection services, string userDataPath)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(sp =>
        {
            // Settled before the file is opened, since opening it is what creates it.
            bool firstRun = !File.Exists(Path.Combine(userDataPath, ControlsFileName));

            ConfigFile<ControlBindings> file = new(
                userDataPath,
                ControlsFileName,
                bindings => bindings.Repair<EliteAction, EliteAxis>(EliteControlDefaults.Create()),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConfigFile<ControlBindings>>());

            // Defaults are written out, not bound over: config binding merges rather than
            // replaces, so a commander could never remove a binding that way.
            ControlBindings bindings = firstRun ? EliteControlDefaults.Create() : file.ReadConfig();
            file.WriteConfig(bindings);

            return bindings;
        });

        services.AddSingleton(sp => EliteControls.Map(
            sp.GetRequiredService<ControlBindings>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<IGamepad>()));

        return services;
    }
}
