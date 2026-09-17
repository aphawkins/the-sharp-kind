// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind;
using SharpKind.Assets;
using StuntCarRacerSharpLib.Rendering;
using StuntCarRacerSharpLib.Tracks;
using Xunit;

namespace StuntCarRacerSharpLib.Tests.Rendering;

public class CarMeshTests
{
    // A level, square wheel-corner frame: right = +X, forward = +Z, up = +Y.
    private static readonly Coord3D s_rearLeft = new(-81, 0, -128);
    private static readonly Coord3D s_rearRight = new(81, 0, -128);
    private static readonly Coord3D s_frontLeft = new(-81, 0, 128);
    private static readonly Coord3D s_frontRight = new(81, 0, 128);

    // Regression lock for the car.obj conversion: same quad count and
    // colours as the original hardcoded CreateCarInVB-derived arrays.
    [Fact]
    public void AppendProducesTenQuadsWithTheOriginalColours()
    {
        ScrPalette palette = new(AssetLocator.Create());
        CarMesh carMesh = new(palette);
        List<WorldPolygon> polygons = [];

        carMesh.Append(polygons, s_rearLeft, s_rearRight, s_frontLeft, s_frontRight);

        Assert.Equal(10, polygons.Count);
        Assert.All(polygons, p => Assert.Equal(4, p.Points.Length));

        FastColor wheelColour = palette.Colour(Track.ScrBaseColour);
        FastColor sideColour = palette.Colour(Track.ScrBaseColour + 12);
        FastColor endColour = palette.Colour(Track.ScrBaseColour + 10);
        FastColor topColour = palette.Colour(Track.ScrBaseColour + 15);
        FastColor bottomColour = palette.Colour(Track.ScrBaseColour + 9);

        Assert.Equal(4, polygons.Count(p => p.Colour == wheelColour));
        Assert.Equal(2, polygons.Count(p => p.Colour == sideColour));
        Assert.Equal(2, polygons.Count(p => p.Colour == endColour));
        Assert.Single(polygons, p => p.Colour == topColour);
        Assert.Single(polygons, p => p.Colour == bottomColour);
    }

    // A level frame here, so the wheel quads' lowest points should land at the corners' Y (0).
    [Fact]
    public void MeshBottomSitsAtCornerLevelOnALevelFrame()
    {
        CarMesh carMesh = new(new ScrPalette(AssetLocator.Create()));
        List<WorldPolygon> polygons = [];

        carMesh.Append(polygons, s_rearLeft, s_rearRight, s_frontLeft, s_frontRight);

        int lowestY = polygons.SelectMany(p => p.Points.ToArray()).Min(c => c.Y);

        Assert.Equal(0, lowestY);
    }

    // Far from the origin and at an angle, where rounding has the most room to show: the car is rigid, so point-to-point distance must not change.
    [Fact]
    public void TheMeshKeepsItsShapeOnAFarOffAngledFrame()
    {
        CarMesh carMesh = new(new ScrPalette(AssetLocator.Create()));
        List<WorldPolygon> atOrigin = [];
        List<WorldPolygon> farAway = [];

        carMesh.Append(atOrigin, s_rearLeft, s_rearRight, s_frontLeft, s_frontRight);

        // The same square frame, turned 45 degrees about y and moved a long
        // way out along the track.
        const int offset = 100_000;
        carMesh.Append(
            farAway,
            Turned(s_rearLeft, offset),
            Turned(s_rearRight, offset),
            Turned(s_frontLeft, offset),
            Turned(s_frontRight, offset));

        Coord3D[] first = [.. atOrigin.SelectMany(p => p.Points.ToArray())];
        Coord3D[] second = [.. farAway.SelectMany(p => p.Points.ToArray())];

        Assert.Equal(first.Length, second.Length);

        // Every edge of every face keeps its length, to within the whole unit
        // the track rounds to.
        for (int i = 1; i < first.Length; i++)
        {
            Assert.Equal(Separation(first[i - 1], first[i]), Separation(second[i - 1], second[i]), 2.0);
        }
    }

    [Fact]
    public void AppendThrowsOnNullPolygons()
    {
        CarMesh carMesh = new(new ScrPalette(AssetLocator.Create()));

        Assert.Throws<ArgumentNullException>(
            () => carMesh.Append(null!, s_rearLeft, s_rearRight, s_frontLeft, s_frontRight));
    }

    private static Coord3D Turned(Coord3D corner, int offset)
    {
        const double angle = Math.PI / 4;
        double x = (corner.X * Math.Cos(angle)) - (corner.Z * Math.Sin(angle));
        double z = (corner.X * Math.Sin(angle)) + (corner.Z * Math.Cos(angle));

        return new((int)x + offset, corner.Y + offset, (int)z + offset);
    }

    private static double Separation(Coord3D first, Coord3D second)
    {
        double dx = first.X - second.X;
        double dy = first.Y - second.Y;
        double dz = first.Z - second.Z;

        return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }
}
