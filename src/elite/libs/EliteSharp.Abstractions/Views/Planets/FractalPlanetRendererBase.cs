// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind;

namespace EliteSharp.Abstractions.Views.Planets;

/// <summary>
/// A planet with a generated surface: midpoint displacement makes a height
/// map, which is then read as land and sea, shaded by distance from a light
/// at the top left.
/// <para>
/// The heights are the game's - the same system renders a byte-identical
/// planet every time, because the game seeds the stream it is handed - and
/// what those heights look like is the rendition's.
/// </para>
/// </summary>
public abstract class FractalPlanetRendererBase(IViewSurface surface, IRandomSource random) : IPlanetRenderer
{
    private readonly PlanetSurface _surface = new(surface);
    private readonly IRandomSource _random = random;

    // Heights, not colours; the colouring pass maps each onto the sphere's colour map.
    private readonly uint[,] _heights =
        new uint[PlanetSurface.LandXMax + 1, PlanetSurface.LandYMax + 1];

    private bool _mapped;

    public void Draw(PlanetView planet)
    {
        // Deferred to first draw: the subclass can't colour anything until construction finishes.
        if (!_mapped)
        {
            GenerateLandscape();
            _mapped = true;
        }

        _surface.Draw(planet.Centre, planet.Radius, planet.Orientation);
    }

    /// <summary>
    /// The colour for one point of the surface.
    /// </summary>
    /// <param name="height">The generated height, 0 to 255.</param>
    /// <param name="isShaded">
    /// Whether the point is far enough from the light to be in shade.
    /// </param>
    /// <returns>The colour that point is drawn in.</returns>
    protected abstract FastColor SurfaceColour(uint height, bool isShaded);

    /// <summary>
    /// Generate a fractal landscape. Uses midpoint displacement method.
    /// </summary>
    private void GenerateLandscape()
    {
        const int d = PlanetSurface.LandXMax / 8;

        for (int y = 0; y <= PlanetSurface.LandYMax; y += d)
        {
            for (int x = 0; x <= PlanetSurface.LandXMax; x += d)
            {
                _heights[x, y] = (uint)_random.Random(255);
            }
        }

        for (int y = 0; y < PlanetSurface.LandYMax; y += d)
        {
            for (int x = 0; x < PlanetSurface.LandXMax; x += d)
            {
                MidpointSquare(x, y, d);
            }
        }

        ColourLandscape();
    }

    /// <summary>
    /// Turn the generated heightmap into a surface, shaded by distance from
    /// the light source at the top left.
    /// </summary>
    private void ColourLandscape()
    {
        for (int y = 0; y <= PlanetSurface.LandYMax; y++)
        {
            for (int x = 0; x <= PlanetSurface.LandXMax; x++)
            {
                float dist = (x * x) + (y * y);
                _surface.Landscape[x, y] = SurfaceColour(_heights[x, y], dist > 10000);
            }
        }
    }

    /// <summary>
    /// Calculate the midpoint between two given points.
    /// </summary>
    private uint CalcMidpointColour(int sx, int sy, int ex, int ey)
        => Math.Clamp(
            ((_heights[sx, sy] + _heights[ex, ey]) / 2) + (uint)_random.GaussianRandom(-7, 8),
            0,
            255);

    /// <summary>
    /// Calculate a square on the midpoint map.
    /// </summary>
    private void MidpointSquare(int tx, int ty, int w)
    {
        int d = w / 2;
        int mx = tx + d;
        int my = ty + d;
        int bx = tx + w;
        int by = ty + w;

        _heights[mx, ty] = CalcMidpointColour(tx, ty, bx, ty);
        _heights[mx, by] = CalcMidpointColour(tx, by, bx, by);
        _heights[tx, my] = CalcMidpointColour(tx, ty, tx, by);
        _heights[bx, my] = CalcMidpointColour(bx, ty, bx, by);
        _heights[mx, my] = CalcMidpointColour(tx, my, bx, my);

        if (d == 1)
        {
            return;
        }

        MidpointSquare(tx, ty, d);
        MidpointSquare(mx, ty, d);
        MidpointSquare(tx, my, d);
        MidpointSquare(mx, my, d);
    }
}
