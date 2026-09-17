// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Composition.Convention;
using System.Composition.Hosting;
using System.Reflection;
using System.Runtime.Loader;
using EliteSharp.Abstractions.Missions;
using Microsoft.Extensions.Logging;

namespace EliteSharpLib.Missions;

/// <summary>
/// Finds the missions in the plugin folder. Everything MEF touches happens in
/// here and is finished with by the time the loader returns: it hands back
/// mission instances, which are then registered like anything else, so the
/// composition host holds nothing and the game keeps its one composition root.
/// </summary>
internal static class MissionLoader
{
    /// <summary>
    /// The folder plugin assemblies are dropped into, beside the executable.
    /// </summary>
    internal const string FolderName = "Missions";

    /// <summary>
    /// Missions are exported by convention rather than by attribute, so that a
    /// plugin references the contracts assembly and nothing else - a mission is
    /// a public class implementing <see cref="IMission"/> with a constructor
    /// taking no arguments, and says nothing about MEF.
    /// </summary>
    private static readonly ConventionBuilder s_conventions = BuildConventions();

    /// <summary>
    /// Loads every mission in the plugin folder. Having no plugins is the
    /// normal case, not a problem, so an absent folder is noted and nothing
    /// else happens.
    /// </summary>
    /// <param name="baseDirectory">
    /// The folder the plugin folder sits in - the executable's, in the game,
    /// and a temporary one in tests.
    /// </param>
    /// <param name="logger">Where skipped files and the count found are reported.</param>
    /// <returns>The missions found, in no particular order.</returns>
    public static IReadOnlyList<IMission> LoadFrom(string baseDirectory, ILogger logger)
    {
        string folder = Path.Combine(baseDirectory, FolderName);

        if (!Directory.Exists(folder))
        {
            LogMessages.NoMissionFolder(logger, folder);
            return [];
        }

        List<Assembly> assemblies = [];

        foreach (string file in Directory.EnumerateFiles(folder, "*.dll"))
        {
            // One unreadable file is one plugin the commander does not get, not a game that fails to start.
            try
            {
                assemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file)));
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException)
            {
                LogMessages.MissionAssemblyUnreadable(logger, file, ex);
            }
        }

        if (assemblies.Count == 0)
        {
            return [];
        }

        using CompositionHost host = new ContainerConfiguration()
            .WithAssemblies(assemblies, s_conventions)
            .CreateContainer();

        IMission[] missions = [.. host.GetExports<IMission>()];
        LogMessages.MissionsLoaded(logger, missions.Length, assemblies.Count);

        return missions;
    }

    private static ConventionBuilder BuildConventions()
    {
        ConventionBuilder conventions = new();
        conventions.ForTypesDerivedFrom<IMission>().Export<IMission>();

        return conventions;
    }
}
