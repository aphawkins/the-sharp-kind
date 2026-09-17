// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;

namespace EliteSharpLib.Graphics;

/// <summary>
/// Where a world - a planet or a sun - lands on screen, and how big. Both are
/// projected the same way and always have been; the arithmetic sat in each of
/// the seven classes that drew one until the renderers moved out to the
/// renditions and left the projection behind as the game's half.
/// </summary>
internal static class WorldProjection
{
    /// <summary>
    /// Projects a world onto the screen.
    /// </summary>
    /// <param name="draw">The game's drawing, for the focal length and viewport.</param>
    /// <param name="location">Where the world is in space.</param>
    /// <param name="centre">Where it lands on screen.</param>
    /// <param name="radius">How big it is on screen, in pixels.</param>
    /// <param name="unitScale">
    /// Pixels per unit of the original's 256-wide space, for a renderer whose
    /// thresholds are written in the original's terms.
    /// </param>
    /// <returns>False when no part of it is in view, in which case nothing is drawn.</returns>
    internal static bool TryProject(
        IEliteDraw draw,
        Vector4 location,
        out Vector2 centre,
        out float radius,
        out float unitScale)
    {
        centre = draw.Projector.Project(location.X, location.Y, location.Z);

        // Planets are BIG! Radius is in the original's 256-wide space, so it follows the focal length, not the scale.
        unitScale = draw.Focus / 256;
        radius = 6291456 / location.Length() * unitScale;

        return centre.X + radius >= draw.Layout.ViewportLeft
            && centre.X - radius <= draw.Layout.ViewportRight
            && centre.Y + radius >= draw.Layout.ViewportTop
            && centre.Y - radius <= draw.Layout.ViewportBottom;
    }
}
