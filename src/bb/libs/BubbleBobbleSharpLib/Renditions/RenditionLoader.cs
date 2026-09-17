// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Composition.Convention;
using System.Composition.Hosting;
using System.Reflection;
using System.Runtime.Loader;
using BubbleBobbleSharp.Abstractions.Renditions;
using Microsoft.Extensions.Logging;

namespace BubbleBobbleSharpLib.Renditions;

/// <summary>
/// Finds the renditions in the plugin folder and picks the one the player
/// configured. Everything MEF touches happens in here and is finished with by
/// the time the loader returns: it hands back a rendition, which is then
/// registered like anything else.
/// <para>
/// A rendition is not optional. A missing Renditions folder leaves the game
/// with nothing to draw with at all, so this fails at startup and says which
/// name it could not find rather than starting a game that cannot show
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
    /// Renditions are exported by convention rather than by attribute, so a
    /// plugin references the contracts assembly and nothing else - a rendition
    /// is a public class implementing <see cref="IBbRendition"/> with a
    /// constructor taking no arguments, and says nothing about MEF.
    /// </summary>
    private static readonly ConventionBuilder s_conventions = BuildConventions();

    /// <summary>
    /// Loads the rendition for one name.
    /// </summary>
    /// <param name="baseDirectory">
    /// The folder the plugin folder sits in - the executable's, in the game,
    /// and a temporary one in tests.
    /// </param>
    /// <param name="name">The name the player configured.</param>
    /// <param name="logger">Where skipped files and the count found are reported.</param>
    /// <returns>The rendition chosen, and everything installed.</returns>
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
            // A rendition is a folder, not a loose file: it brings its own
            // artwork, palette and font alongside its code, and a second
            // rendition's would collide with the first's in one directory.
            // Loose DLLs are still read, so a code-only rendition needs no
            // folder of its own.
            foreach (string file in Directory.EnumerateFiles(renditionsFolder, "*.dll", SearchOption.AllDirectories))
            {
                // One unreadable file is one rendition the player cannot use,
                // which is only fatal if it was the one they asked for - so
                // the decision is left to the search below.
                try
                {
                    assemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file)));
                }
                catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException)
                {
                    LogMessages.RenditionAssemblyUnreadable(logger, file, ex);
                }
            }
        }

        IBbRendition[] renditions = [];

        if (assemblies.Count > 0)
        {
            using CompositionHost host = new ContainerConfiguration()
                .WithAssemblies(assemblies, s_conventions)
                .CreateContainer();

            renditions = [.. host.GetExports<IBbRendition>()];
        }

        LogMessages.RenditionsLoaded(logger, renditions.Length, assemblies.Count, name);

        IBbRendition chosen = Array.Find(renditions, rendition => string.Equals(rendition.Name, name, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Nothing in '{renditionsFolder}' is called '{name}', so there is nothing to draw the game with.");

        // Where it came from, so the game can find the artwork it brought with
        // it. A rendition loaded from a loose DLL has the Renditions folder
        // itself, which is the right answer for one shipping no assets.
        string folder = Path.GetDirectoryName(chosen.GetType().Assembly.Location) ?? renditionsFolder;

        return new(chosen, folder, [.. renditions.OrderBy(r => r.Name, StringComparer.Ordinal)]);
    }

    private static ConventionBuilder BuildConventions()
    {
        ConventionBuilder conventions = new();
        conventions.ForTypesDerivedFrom<IBbRendition>().Export<IBbRendition>();

        return conventions;
    }
}
