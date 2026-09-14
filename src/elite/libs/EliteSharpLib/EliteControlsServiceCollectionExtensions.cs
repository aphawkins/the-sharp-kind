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

// The bindings file and the map built from it. Its own class because
// EliteServiceCollectionExtensions is already at CA1506's class-coupling
// limit (96), the same reason the split-screen registrations live apart -
// the metric is per class, so an extra static class is what resolves it.
public static class EliteControlsServiceCollectionExtensions
{
    // The bindings live in their own file rather than inside elite.sharp:
    // they are a long list a commander edits by hand, and keeping them apart
    // means a mistake in one cannot cost the other.
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
            // Whether this is a first run is settled before the file is
            // opened, because opening it is what creates it.
            bool firstRun = !File.Exists(Path.Combine(userDataPath, ControlsFileName));

            ConfigFile<ControlBindings> file = new(
                userDataPath,
                ControlsFileName,
                bindings => bindings.Repair<EliteAction, EliteAxis>(EliteControlDefaults.Create()),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConfigFile<ControlBindings>>());

            // The defaults are written out rather than bound over: a file
            // that does not exist yet binds to an empty ControlBindings, and
            // empty means every control dead. They cannot live in the
            // properties either - configuration binding merges into what is
            // already there, so a commander could never remove a binding.
            //
            // An existing file is read as it stands, repaired by ConfigFile
            // as it goes, and written straight back, so a hand-edit survives:
            // what goes back is what was just read.
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
