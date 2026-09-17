// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Composition.Convention;
using System.Composition.Hosting;
using System.Reflection;
using System.Runtime.Loader;
using EliteSharp.Abstractions.Renditions;
using Microsoft.Extensions.Logging;

namespace EliteSharpLib.Renditions;

/// <summary>
/// Finds the renditions in the plugin folder and picks the one the commander
/// configured. Everything MEF touches happens in here and is finished with by
/// the time the loader returns, the same as
/// <see cref="Missions.MissionLoader"/>: it hands back a rendition, which is
/// then registered like anything else.
/// <para>
/// Unlike a mission, a rendition is not optional. A missing Missions folder
/// costs the commander some missions; a missing Renditions folder leaves the
/// game with nothing to draw with at all, so this fails at startup and says
/// which name it could not find rather than starting a game that cannot show
/// itself.
/// </para>
/// </summary>
internal static class RenditionLoader
{
    /// <summary>
    /// The folder plugin assemblies are dropped into, beside the executable.
    /// </summary>
    internal const string FolderName = "Renditions";

    /// <summary>
    /// Renditions are exported by convention rather than by attribute, so that a
    /// plugin references the contracts assembly and nothing else - a rendition is a
    /// public class implementing <see cref="IRendition"/> with a constructor
    /// taking no arguments, and says nothing about MEF.
    /// </summary>
    private static readonly ConventionBuilder s_conventions = BuildConventions();

    /// <summary>
    /// Loads the rendition for one name.
    /// </summary>
    /// <param name="baseDirectory">
    /// The folder the plugin folder sits in - the executable's, in the game,
    /// and a temporary one in tests.
    /// </param>
    /// <param name="name">The name the commander configured.</param>
    /// <param name="logger">Where skipped files and the count found are reported.</param>
    /// <returns>The rendition chosen, and the names of everything installed.</returns>
    /// <exception cref="InvalidOperationException">
    /// Nothing in the folder goes by this name, which the game cannot start
    /// without.
    /// </exception>
    public static InstalledRenditions LoadFrom(string baseDirectory, string name, ILogger logger)
    {
        string renditionsFolder = Path.Combine(baseDirectory, FolderName);
        List<Assembly> assemblies = [];

        if (Directory.Exists(renditionsFolder))
        {
            // A rendition is a folder, not a loose file: its artwork, palette, fonts and models would collide with another rendition's in one directory.
            // Loose DLLs are still read, so a code-only rendition needs no folder of its own.
            foreach (string file in Directory.EnumerateFiles(renditionsFolder, "*.dll", SearchOption.AllDirectories))
            {
                // One unreadable file is one rendition the commander cannot play; fatal only if it was the one they asked for, decided below.
                try
                {
                    assemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file)));
                }
                catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException)
                {
                    LogMessages.ViewAssemblyUnreadable(logger, file, ex);
                }
            }
        }

        IRendition[] renditions = [];

        if (assemblies.Count > 0)
        {
            using CompositionHost host = new ContainerConfiguration()
                .WithAssemblies(assemblies, s_conventions)
                .CreateContainer();

            renditions = [.. host.GetExports<IRendition>()];
        }

        LogMessages.RenditionsLoaded(logger, renditions.Length, assemblies.Count, name);

        IRendition chosen = Array.Find(renditions, rendition => string.Equals(rendition.Name, name, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Nothing in '{renditionsFolder}' is called '{name}', so there is nothing to draw the game with.");

        // Where it came from, so the game can find its artwork. A loose-DLL rendition gets the Renditions folder itself, the right answer for one shipping no assets.
        string folder = Path.GetDirectoryName(chosen.GetType().Assembly.Location) ?? renditionsFolder;

        // The settings screen offers the commander what is installed, so the ones not chosen are worth keeping too.
        return new(chosen, folder, [.. renditions.OrderBy(r => r.Name, StringComparer.Ordinal)]);
    }

    private static ConventionBuilder BuildConventions()
    {
        ConventionBuilder conventions = new();
        conventions.ForTypesDerivedFrom<IRendition>().Export<IRendition>();

        return conventions;
    }
}
