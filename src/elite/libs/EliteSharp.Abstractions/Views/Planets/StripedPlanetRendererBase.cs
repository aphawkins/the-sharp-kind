// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind;

namespace EliteSharp.Abstractions.Views.Planets;

/// <summary>
/// A banded, Jupiter-like planet. The banding is shared; the stripes are the
/// rendition's, and how many there are with them - a rendition with a ramp to
/// grade through can afford far more bands than one with sixteen colours.
/// </summary>
public abstract class StripedPlanetRendererBase(IViewSurface surface) : IPlanetRenderer
{
    private readonly PlanetSurface _surface = new(surface);
    private bool _mapped;

    /// <summary>
    /// Gets the bands, pole to pole. They are spread evenly over the sphere,
    /// so the count sets how fine the banding is.
    /// </summary>
    protected abstract IReadOnlyList<FastColor> Stripes { get; }

    public void Draw(PlanetView planet)
    {
        // Deferred to first draw: the subclass's stripes don't exist until construction finishes.
        if (!_mapped)
        {
            MapStripes();
            _mapped = true;
        }

        _surface.Draw(planet.Centre, planet.Radius, planet.Orientation);
    }

    private void MapStripes()
    {
        IReadOnlyList<FastColor> stripes = Stripes;

        for (int y = 0; y <= PlanetSurface.LandYMax; y++)
        {
            FastColor colour = stripes[y * (stripes.Count - 1) / PlanetSurface.LandYMax];

            for (int x = 0; x <= PlanetSurface.LandXMax; x++)
            {
                _surface.Landscape[x, y] = colour;
            }
        }
    }
}
