// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharp.Abstractions.Renditions;

/// <summary>
/// Turns the window scale held in the config file into one the chosen
/// rendition actually offers. The config cannot validate this itself: what is
/// on offer is the rendition's, and the rendition is not known until it has
/// been loaded.
/// </summary>
public static class WindowScales
{
    /// <summary>
    /// Resolves the configured scale against a rendition. An unchosen scale
    /// becomes the rendition's default; anything else is pegged to the nearest
    /// scale on offer, so a hand-edited file costs the commander the exact
    /// number rather than the whole setting.
    /// </summary>
    /// <param name="rendition">The rendition the game will draw itself with.</param>
    /// <param name="configured">The scale the config file holds, or null if it holds none.</param>
    /// <returns>A scale the rendition offers.</returns>
    public static int Resolve(IRendition rendition, int? configured)
    {
        ArgumentNullException.ThrowIfNull(rendition);

        if (configured is not int scale)
        {
            return rendition.DefaultWindowScale;
        }

        int nearest = rendition.WindowScales[0];
        foreach (int offered in rendition.WindowScales)
        {
            // Ties go to the larger: the ascending list makes a later equal-distance entry the bigger one.
            if (Math.Abs(offered - scale) <= Math.Abs(nearest - scale))
            {
                nearest = offered;
            }
        }

        return nearest;
    }
}
