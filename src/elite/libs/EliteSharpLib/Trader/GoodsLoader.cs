// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Composition.Convention;
using System.Composition.Hosting;
using System.Reflection;
using System.Runtime.Loader;
using EliteSharp.Abstractions.Trading;
using Microsoft.Extensions.Logging;

namespace EliteSharpLib.Trader;

/// <summary>
/// Finds the goods set in the plugin folder. Everything MEF touches happens in
/// here and is finished with by the time the loader returns, the same as
/// <see cref="Missions.MissionLoader"/> and
/// <see cref="Renditions.RenditionLoader"/>: it hands back one
/// <see cref="IGoodsSet"/>, which is then registered like anything else.
/// <para>
/// Unlike a mission and like a rendition, a goods set is not optional: there is
/// no market without one, so an absent folder or an empty one is a startup
/// failure that says what it could not find rather than a game that cannot
/// trade. Two sets installed is also fatal - the game trades one economy, and
/// which assembly to remove is the only useful thing to say.
/// </para>
/// </summary>
internal static class GoodsLoader
{
    /// <summary>
    /// The folder plugin assemblies are dropped into, beside the executable.
    /// </summary>
    internal const string FolderName = "Goods";

    private static readonly ConventionBuilder s_conventions = BuildConventions();

    /// <summary>
    /// Loads the one goods set in the plugin folder.
    /// </summary>
    /// <param name="baseDirectory">
    /// The folder the plugin folder sits in - the executable's, in the game,
    /// and a temporary one in tests.
    /// </param>
    /// <param name="logger">Where skipped files and the set found are reported.</param>
    /// <returns>The goods set the game will trade.</returns>
    /// <exception cref="InvalidOperationException">
    /// The folder is absent or holds no goods set, or holds more than one.
    /// </exception>
    public static IGoodsSet LoadFrom(string baseDirectory, ILogger logger)
    {
        string folder = Path.Combine(baseDirectory, FolderName);
        List<Assembly> assemblies = [];

        if (Directory.Exists(folder))
        {
            foreach (string file in Directory.EnumerateFiles(folder, "*.dll"))
            {
                // One unreadable file is not fatal on its own; the search below decides.
                try
                {
                    assemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file)));
                }
                catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException)
                {
                    LogMessages.GoodsAssemblyUnreadable(logger, file, ex);
                }
            }
        }

        IGoodsSet[] sets = [];

        if (assemblies.Count > 0)
        {
            using CompositionHost host = new ContainerConfiguration()
                .WithAssemblies(assemblies, s_conventions)
                .CreateContainer();

            sets = [.. host.GetExports<IGoodsSet>()];
        }

        return sets switch
        {
            [IGoodsSet only] => Chosen(only, sets.Length, assemblies.Count, logger),
            [] => throw new InvalidOperationException(
                $"Nothing in '{folder}' is a goods set, so the game has nothing to trade."),
            _ => throw new InvalidOperationException(
                $"'{folder}' holds {sets.Length} goods sets ({string.Join(", ", sets.Select(set => set.Name))}); "
                    + "the game trades one. Remove all but one."),
        };
    }

    private static IGoodsSet Chosen(IGoodsSet set, int setCount, int assemblyCount, ILogger logger)
    {
        LogMessages.GoodsLoaded(logger, setCount, assemblyCount, set.Name);
        return set;
    }

    private static ConventionBuilder BuildConventions()
    {
        ConventionBuilder conventions = new();
        conventions.ForTypesDerivedFrom<IGoodsSet>().Export<IGoodsSet>();

        return conventions;
    }
}
