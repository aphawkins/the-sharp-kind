// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using SharpKind;

namespace EliteSharp.Abstractions.Views.Planets;

/// <summary>
/// Wraps a colour map round a sphere and rasterises it - the shared
/// machinery every surfaced planet style draws through, whatever colours it
/// fills the map with. Based on Doros circle drawing algorithm.
/// </summary>
public sealed class PlanetSurface
{
    // The colour map is a square of this many samples per side.
    internal const int LandXMax = 128;

    internal const int LandYMax = 128;

    private readonly IViewSurface _draw;

    public PlanetSurface(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _draw = surface;
    }

    // A rendition supplies colours through one of the style bases rather than filling this itself.
    internal FastColor[,] Landscape { get; } = new FastColor[LandXMax + 1, LandYMax + 1];

    /// <summary>
    /// Draws the sphere with the colour map wrapped round it.
    /// </summary>
    /// <param name="centre">The planet's centre on screen.</param>
    /// <param name="radius">Its radius in pixels.</param>
    /// <param name="rotmat">Which way up it is.</param>
    public void Draw(Vector2 centre, float radius, Matrix4x4 rotmat)
    {
        float vx = rotmat.M21 * 65536;
        float vy = rotmat.M22 * 65536;
        float x = MathF.Floor(radius);
        float s = -x;
        float y = 0;

        while (y <= x)
        {
            // Top of top half
            RenderPlanetLine(centre, y, -x, radius, vx, vy);

            // Bottom of top half
            RenderPlanetLine(centre, x, -y, radius, vx, vy);

            // Top of bottom half
            RenderPlanetLine(centre, x, y, radius, vx, vy);

            // Bottom of bottom half
            RenderPlanetLine(centre, y, x, radius, vx, vy);

            s += y + y + 1;
            y++;
            if (s >= 0)
            {
                s -= x + x + 2;
                x--;
            }
        }
    }

    /// <summary>
    /// Draw a line of the planet with appropriate rotation.
    /// </summary>
    private void RenderPlanetLine(Vector2 centre, float offsetX, float offsetY, float radius, float vx, float vy)
    {
        Vector2 s = new()
        {
            Y = offsetY + centre.Y,
        };

        // The 8-bit border has no bottom edge, so the planet fills right down to the scanner, as suns do.
        if (s.Y < _draw.Layout.ViewportTop || s.Y > _draw.Layout.ViewportBottom)
        {
            return;
        }

        s.X = centre.X - offsetX;
        float ex = centre.X + offsetX;

        float rx = (-offsetX * vx) - (offsetY * vy);
        float ry = (-offsetX * vy) + (offsetY * vx);
        rx += radius * 65536;
        ry += radius * 65536;

        // radius * 2 * LAND_X_MAX >> 16
        float div = radius * 1024;

        for (; s.X <= ex; s.X++)
        {
            if (s.X >= _draw.Layout.ViewportLeft && s.X < _draw.Layout.ViewportRight)
            {
                int lx = (int)Math.Clamp(MathF.Abs(rx / div), 0, LandXMax);
                int ly = (int)Math.Clamp(MathF.Abs(ry / div), 0, LandYMax);
                _draw.Graphics.DrawPixel(s, Landscape[lx, ly]);
            }

            rx += vx;
            ry += vy;
        }
    }
}
