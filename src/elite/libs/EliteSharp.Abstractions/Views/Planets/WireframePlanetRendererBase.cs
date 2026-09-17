// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using SharpKind;

namespace EliteSharp.Abstractions.Views.Planets;

/// <summary>
/// A planet as an outline: a circle, and either a crater or an equator and
/// meridian mapped from its orientation. The geometry is shared - it is the
/// original's, and derived from the radius - and the line colour is the
/// rendition's.
/// </summary>
public abstract class WireframePlanetRendererBase : IPlanetRenderer
{
    // The original draws no surface detail below a radius of 6 (PL9).
    private const float MinDetailRadius = 6;

    // Segments per half ellipse; the original steps a 64-segment circle by 4.
    private const int HalfEllipseSegments = 8;

    // Offset from the planet's centre along roofv, 222/256 of the radius (PLS3).
    private const float CraterOffset = 222f / 256;

    private readonly IViewSurface _surface;
    private readonly bool _hasCrater;

    protected WireframePlanetRendererBase(IViewSurface surface, bool hasCrater)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _surface = surface;
        _hasCrater = hasCrater;
    }

    /// <summary>
    /// Gets the colour the outline is drawn in.
    /// </summary>
    protected abstract FastColor Colour { get; }

    public void Draw(PlanetView planet)
    {
        _surface.Graphics.DrawCircle(planet.Centre, planet.Radius, Colour);

        // The threshold is in the original's 256-wide space, scaled to pixels the same way the radius was.
        if (planet.Radius < MinDetailRadius * planet.UnitScale)
        {
            return;
        }

        Vector2 centre = planet.Centre;
        float radius = planet.Radius;

        // The orientation vector rows, as the original's sidev/roofv/nosev.
        Vector3 sidev = new(planet.Orientation.M11, planet.Orientation.M12, planet.Orientation.M13);
        Vector3 roofv = new(planet.Orientation.M21, planet.Orientation.M22, planet.Orientation.M23);
        Vector3 nosev = new(planet.Orientation.M31, planet.Orientation.M32, planet.Orientation.M33);

        if (_hasCrater)
        {
            DrawCrater(centre, radius, sidev, roofv, nosev);
        }
        else
        {
            DrawEquatorAndMeridian(centre, radius, sidev, roofv, nosev);
        }
    }

    /// <summary>
    /// The starting angle of a meridian, as the original's PLS4:
    /// arctan(-nosev_z / other_z), reduced to half a turn, plus half a turn
    /// again when nosev_z is positive.
    /// </summary>
    private static float MeridianStartAngle(float nosevZ, float otherZ)
    {
        float angle = MathF.Atan2(-nosevZ, otherZ);

        if (angle < 0)
        {
            angle += MathF.PI;
        }

        return nosevZ >= 0 ? angle + MathF.PI : angle;
    }

    /// <summary>
    /// A point on the ellipse defined by the centre and the two conjugate
    /// radius vectors u and v (PLS22). The y components are negated because
    /// screen y runs the opposite way to y in space.
    /// </summary>
    private static Vector2 EllipsePoint(Vector2 centre, Vector2 u, Vector2 v, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);

        return new(
            centre.X + (u.X * cos) + (v.X * sin),
            centre.Y - (u.Y * cos) - (v.Y * sin));
    }

    /// <summary>
    /// Draw the planet's equator and meridian: two half ellipses sharing
    /// nosev, one against roofv and one against sidev (PL9 part 2).
    /// </summary>
    private void DrawEquatorAndMeridian(Vector2 centre, float radius, Vector3 sidev, Vector3 roofv, Vector3 nosev)
    {
        Vector2 u = new(nosev.X * radius, nosev.Y * radius);

        DrawEllipseArc(
            centre,
            u,
            new(roofv.X * radius, roofv.Y * radius),
            MeridianStartAngle(nosev.Z, roofv.Z),
            MathF.PI,
            HalfEllipseSegments);

        DrawEllipseArc(
            centre,
            u,
            new(sidev.X * radius, sidev.Y * radius),
            MeridianStartAngle(nosev.Z, sidev.Z),
            MathF.PI,
            HalfEllipseSegments);
    }

    /// <summary>
    /// Draw the planet's crater: a full ellipse of half the planet's radius,
    /// offset along roofv (PL9 part 3).
    /// </summary>
    private void DrawCrater(Vector2 centre, float radius, Vector3 sidev, Vector3 roofv, Vector3 nosev)
    {
        if (roofv.Z < 0)
        {
            // The crater is on the far side of the planet.
            return;
        }

        Vector2 craterCentre = new(
            centre.X + (roofv.X * radius * CraterOffset),
            centre.Y - (roofv.Y * radius * CraterOffset));

        DrawEllipseArc(
            craterCentre,
            new(nosev.X * radius / 2, nosev.Y * radius / 2),
            new(sidev.X * radius / 2, sidev.Y * radius / 2),
            0,
            MathF.Tau,
            HalfEllipseSegments * 2);
    }

    private void DrawEllipseArc(Vector2 centre, Vector2 u, Vector2 v, float startAngle, float sweep, int segments)
    {
        Vector2 start = EllipsePoint(centre, u, v, startAngle);

        for (int i = 1; i <= segments; i++)
        {
            Vector2 end = EllipsePoint(centre, u, v, startAngle + (sweep * i / segments));
            _surface.Graphics.DrawLine(start, end, Colour);
            start = end;
        }
    }
}
