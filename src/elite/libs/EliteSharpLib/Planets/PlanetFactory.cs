// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Renditions;
using EliteSharp.Abstractions.Views.Planets;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;
using SharpKind;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib.Planets;

internal static class PlanetFactory
{
    // A wireframe world overrides the planet style; the original picks crater vs equator-and-meridian from bit 1 of the system's tech level (SOS1).
    // Which style applies is the game's decision (from settings); what it looks like is the rendition's.
    internal static IObject Create(
        FillMode style,
        PlanetType type,
        IEliteDraw draw,
        IRendition rendition,
        int seed,
        int techLevel)
    {
        ArgumentNullException.ThrowIfNull(rendition);

        PlanetStyle planetStyle = style == FillMode.Wireframe
            ? PlanetStyle.Wireframe
            : type switch
            {
                PlanetType.Fractal => PlanetStyle.Fractal,
                PlanetType.Solid => PlanetStyle.Solid,
                PlanetType.Striped => PlanetStyle.Striped,
                _ => throw new EliteException(),
            };

        // Reseeds a single stream for the whole landscape (fesh0r/newkind's generate_fractal_landscape), so the same system always renders the same planet.
        Random random = new(seed);
        PlanetLook look = new(planetStyle, (techLevel & 2) != 0, new RandomSource(random));

        return new Planet(draw, rendition.CreatePlanetRenderer(draw, look), planetStyle == PlanetStyle.Wireframe);
    }
}
