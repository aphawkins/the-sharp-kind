// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views.Planets;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;

namespace EliteSharpLib.Planets;

/// <summary>
/// A planet in the universe. It has a position, it turns, it is cloned into
/// witchspace and back - all of which is the game's - and what it looks like
/// is the rendition's, through <see cref="IPlanetRenderer"/>.
/// <para>
/// There is one of these whatever the style. The styles used to be four
/// classes here, each repeating the same position and cloning around a
/// different few lines of drawing.
/// </para>
/// </summary>
internal sealed class Planet : IObject
{
    private readonly IEliteDraw _draw;
    private readonly IPlanetRenderer _renderer;

    internal Planet(IEliteDraw draw, IPlanetRenderer renderer, bool spins)
    {
        _draw = draw;
        _renderer = renderer;

        if (spins)
        {
            // Pitch and roll pegged at 127, as the original (SOS1), so the surface detail keeps sweeping round.
            // Only the outlined style does this: the surfaced styles map detail from Rotmat's rows and expect it to stay put.
            RotX = 127;
            RotZ = 127;
        }
    }

    private Planet(Planet other)
    {
        _draw = other._draw;
        _renderer = other._renderer;
    }

    public string Id { get; set; } = ObjectIds.Planet;

    public ShipTraits Traits { get; set; } = ShipTraits.Stellar;

    public ShipProperties Flags { get; set; }

    public Vector4 Location { get; set; } = new(0, 0, 123456, 0);

    public Matrix4x4 Rotmat { get; set; }

    public float RotX { get; set; }

    public float RotZ { get; set; }

    public IObject Clone()
    {
        Planet planet = new(this);
        this.CopyTo(planet);
        return planet;
    }

    public void Draw()
    {
        if (WorldProjection.TryProject(_draw, Location, out Vector2 centre, out float radius, out float unitScale))
        {
            _renderer.Draw(new(centre, radius, Rotmat, unitScale));
        }
    }
}
