// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharpLib.Fakes;
using EliteSharpLib.Ships;
using SharpKind;
using SharpKind.Assets.Models;

namespace EliteSharpLib.Tests;

public class ShipBaseTests
{
    private const float Tolerance = 0.01f;

    [Fact]
    public void DrawTransformsModelPointsUsingRotmatBasisVectors()
    {
        // Non-orthonormal Rotmat with distinct values, so every basis vector's contribution is independently observable.
        Matrix4x4 rotmat = new(
            1.0f,
            0.2f,
            0.3f,
            0,
            0.4f,
            1.0f,
            0.5f,
            0,
            0.6f,
            0.7f,
            1.0f,
            0,
            0,
            0,
            0,
            0);

        // Near enough that the model still draws its detail lines.
        Vector4 location = new(100, 200, 500, 0);
        Vector4 pointA = new(10, 20, 30, 0);
        Vector4 pointB = new(5, -15, 25, 0);

        Point modelPointA = new() { Coords = pointA, FaceNormals = [] };
        Point modelPointB = new() { Coords = pointB, FaceNormals = [] };

        // A rootless 2-point face has no normal to cull against, keeping this test independent of visibility.
        Face face = new() { Color = default, Points = [modelPointA, modelPointB], PointIndices = [0, 1] };

        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = rotmat,
            Location = location,
            Model = new()
            {
                FaceNormals = [],
                Faces = [face],
                Lines = [],
                Points = [modelPointA, modelPointB],
            },
        };

        // Act
        ship.Draw();

        // Assert: p.X*Rotmat[0] + p.Y*Rotmat[1] + p.Z*Rotmat[2] + Location, regardless of Matrix4x4 storage.
        Vector2 expectedA = ProjectUsingRotmatBasis(pointA, rotmat, location, draw);
        Vector2 expectedB = ProjectUsingRotmatBasis(pointB, rotmat, location, draw);

        (Vector2[] points, float[] _, FastColor _, float _) = Assert.Single(draw.DrawnPolygons);
        Assert.Equal(2, points.Length);

        AssertVector2AlmostEqual(expectedA, points[0]);
        AssertVector2AlmostEqual(expectedB, points[1]);
    }

    [Fact]
    public void DrawLasersProjectsAlongFiringDirectionAndClipsToViewBoundary()
    {
        // A mount offset up-and-left of the nose, on a ship right of centre, so the trajectory exits through the top.
        Vector4 location = new(500, 0, 1000, 0);
        Vector4 mountCoords = new(10, 20, 50, 0);
        Point mountPoint = new() { Coords = mountCoords, FaceNormals = [] };

        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = location,
            LaserFront = 0,
            Flags = ShipProperties.Firing,
            Model = new()
            {
                FaceNormals = [],
                Faces = [],
                Lines = [],
                Points = [mountPoint],
            },
        };

        // Act
        ship.Draw();

        // Assert: replicate the production projection, then find where that ray leaves the view rectangle.
        Vector2 expectedMount = Project(mountCoords, location, draw);
        Vector2 expectedAim = Project(mountCoords * 1_000_000f, location, draw);
        Vector2 direction = expectedAim - expectedMount;

        float exitViaLeft = (draw.Layout.ViewportLeft - expectedMount.X) / direction.X;
        float exitViaTop = (draw.Layout.ViewportTop - expectedMount.Y) / direction.Y;
        float exitDistance = MathF.Min(exitViaLeft, exitViaTop);
        Vector2 expectedEnd = expectedMount + (direction * exitDistance);

        (Vector2[] points, float[] _, FastColor _, float _) = Assert.Single(draw.DrawnPolygons);
        Assert.Equal(2, points.Length);
        AssertVector2AlmostEqual(expectedMount, points[0]);
        AssertVector2AlmostEqual(expectedEnd, points[1]);

        // Pins the fix: X lands well away from either screen edge, not clamped to one.
        Assert.True(MathF.Abs(expectedEnd.X) > 1f);
        Assert.True(MathF.Abs(expectedEnd.X - 511) > 1f);
    }

    [Fact]
    public void DrawSubmitsTheCameraDepthOfEachPointOfATiltedFace()
    {
        // A triangle steeply angled to the camera so its three points sit at three clearly different depths.
        Vector4 location = new(0, 0, 1000, 0);
        Vector4 pointA = new(-100, -100, 0, 0);
        Vector4 pointB = new(100, -100, 300, 0);
        Vector4 pointC = new(0, 100, 600, 0);

        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = location,

            // Wound so the face survives the backface cull.
            Model = BuildModel([pointA, pointB, pointC], [[0, 2, 1]]),
        };

        // Act
        ship.Draw();

        // Assert
        (Vector2[] points, float[] depths, FastColor _, float _) = Assert.Single(draw.DrawnPolygons);
        Assert.Equal(3, points.Length);
        Assert.Equal([1000f, 1600f, 1300f], depths);
    }

    [Fact]
    public void DrawBiasesADecalNearerThanTheFaceItSitsOn()
    {
        // A small triangle exactly in the plane of a larger one, as a cockpit window sits in the hull's plane.
        Vector4 location = new(0, 0, 1000, 0);
        Vector4[] points =
        [
            new(-100, -100, 0, 0),
            new(100, -100, 0, 0),
            new(0, 100, 0, 0),
            new(-10, -10, 0, 0),
            new(10, -10, 0, 0),
            new(0, 10, 0, 0),
        ];

        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = location,

            // Wound so both faces survive the backface cull.
            Model = BuildModel(points, [[0, 2, 1], [3, 5, 4]]),
        };

        // Act
        ship.Draw();

        // Assert
        Assert.Equal(2, draw.DrawnPolygons.Count);
        Assert.Equal([1000f, 1000f, 1000f], draw.DrawnPolygons[0].Depths);
        Assert.All(draw.DrawnPolygons[1].Depths, d => Assert.InRange(d, 990f, 999.9f));

        // The decal keeps its base face's whole-face key so the painter's strategy still ties.
        Assert.Equal(draw.DrawnPolygons[0].Z, draw.DrawnPolygons[1].Z);
    }

    [Theory]

    // Far off each side, far above/below, and beyond the far plane - well outside the frustum at depth 1000.
    [InlineData(2000, 0, 1000)]
    [InlineData(-2000, 0, 1000)]
    [InlineData(0, 2000, 1000)]
    [InlineData(0, -2000, 1000)]
    [InlineData(0, 0, 100000)]
    [InlineData(0, 0, -1000)]
    public void DrawSkipsAShipTheViewportCannotShow(float x, float y, float z)
    {
        // Arrange
        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = new(x, y, z, 0),
            Model = BuildModel(
                [new(-100, -100, 0, 0), new(100, -100, 0, 0), new(0, 100, 0, 0)],
                [[0, 2, 1]]),
        };

        // Act
        ship.Draw();

        // Assert
        Assert.Empty(draw.DrawnPolygons);
    }

    [Fact]
    public void DrawKeepsAShipOnlyPartlyOnScreen()
    {
        // The origin sits just outside the right edge, so only the bounding sphere reaches back into view.
        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = new(540, 0, 1000, 0),
            Model = BuildModel(
                [new(-100, -100, 0, 0), new(100, -100, 0, 0), new(0, 100, 0, 0)],
                [[0, 2, 1]]),
        };

        // Act
        ship.Draw();

        // Assert
        Assert.NotEmpty(draw.DrawnPolygons);
    }

    [Fact]
    public void DrawKeepsAFrontFacingFaceThatStraddlesTheCameraPlane()
    {
        // One point behind the camera plane, two well in front - pins the camera-space cull against a
        // winding test on the clamped projection, which would land the behind point at view centre and cull it.
        Vector4 pointA = new(0, 0, -100, 0);
        Vector4 pointB = new(100, 0, 100, 0);
        Vector4 pointC = new(0, 100, 100, 0);

        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = Vector4.Zero,
            Model = BuildModel([pointA, pointB, pointC], [[0, 1, 2]]),
        };

        // Act
        ship.Draw();

        // Assert: drawn, and clipped to the near plane rather than including the behind-camera point.
        (Vector2[] points, float[] depths, FastColor _, float _) = Assert.Single(draw.DrawnPolygons);
        Assert.Equal(4, points.Length);
        Assert.All(depths, d => Assert.True(d > 0));
    }

    [Fact]
    public void DrawCullsADetailLineLyingOnABackFacingFace()
    {
        // A back-facing triangle with a 2-point detail line in its plane, as hull detail sits on a hull face.
        Vector4 location = new(0, 0, 1000, 0);
        Vector4[] points =
        [
            new(-100, -100, 0, 0),
            new(100, -100, 0, 0),
            new(0, 100, 0, 0),
            new(-10, -10, 0, 0),
            new(10, -10, 0, 0),
        ];

        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = location,
            Model = BuildModel(points, [[0, 1, 2], [3, 4]]),
        };

        // Act
        ship.Draw();

        // Assert
        Assert.Empty(draw.DrawnPolygons);
    }

    // A rootless detail line's visibility follows the faces its two ends share.
    [Theory]
    [InlineData(0, 0, 1, false)]
    [InlineData(0, 0, -1, true)]
    public void DrawCullsAnUnrootedDetailLineByTheNormalItsEndsShare(
        float normalX,
        float normalY,
        float normalZ,
        bool expectDrawn)
    {
        // Arrange
        FaceNormal shared = new() { Direction = new(normalX, normalY, normalZ, 0), Visible = true };
        Point start = new() { Coords = new(-10, 0, 0, 0), FaceNormals = [shared] };
        Point end = new() { Coords = new(10, 0, 0, 0), FaceNormals = [shared] };

        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,

            // Near enough that the line counts as visible detail.
            Location = new(0, 0, 300, 0),
            Model = new()
            {
                FaceNormals = [shared],
                Faces = [new() { Color = default, Points = [start, end], PointIndices = [0, 1] }],
                Lines = [],
                Points = [start, end],
            },
        };

        // Act
        ship.Draw();

        // Assert
        Assert.Equal(expectDrawn, draw.DrawnPolygons.Count == 1);
    }

    // The bolt bypasses the face loop entirely, so nothing else culls a mount facing away.
    [Theory]
    [InlineData(0, 0, 1, false)]
    [InlineData(0, 0, -1, true)]
    public void DrawLasersCullsABoltWhoseMountFacesAway(
        float normalX,
        float normalY,
        float normalZ,
        bool expectDrawn)
    {
        // Arrange
        FaceNormal mountNormal = new() { Direction = new(normalX, normalY, normalZ, 0), Visible = true };
        Point mountPoint = new() { Coords = new(10, 20, 50, 0), FaceNormals = [mountNormal] };

        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = new(500, 0, 1000, 0),
            LaserFront = 0,
            Flags = ShipProperties.Firing,
            Model = new()
            {
                FaceNormals = [mountNormal],
                Faces = [],
                Lines = [],
                Points = [mountPoint],
            },
        };

        // Act
        ship.Draw();

        // Assert
        Assert.Equal(expectDrawn, draw.DrawnPolygons.Count == 1);
    }

    // A decal in a hull face's plane must be excluded from that face's corner averaging - it would
    // weight the same plane twice.
    [Fact]
    public void CornerNormalsExcludeADecalFromTheAveraging()
    {
        // Arrange: a small triangle exactly in the plane of a larger one.
        Vector4[] points =
        [
            new(-100, -100, 0, 0),
            new(100, -100, 0, 0),
            new(0, 100, 0, 0),
            new(-10, -10, 0, 0),
            new(10, -10, 0, 0),
            new(0, 10, 0, 0),
        ];

        FakeShip ship = new(new FakeEliteDraw())
        {
            Rotmat = Matrix4x4.Identity,
            Location = new(0, 0, 1000, 0),
            Model = BuildModel(points, [[0, 2, 1], [3, 5, 4]]),
        };

        // Act
        ship.Draw();

        // Assert: hull keeps its own normal at every corner; the decal has none to smooth.
        Assert.All(ship.CornerNormals[0], n => AssertVector3AlmostEqual(new(0, 0, -1), n));
        Assert.All(ship.CornerNormals[1], n => Assert.Equal(Vector3.Zero, n));
    }

    [Fact]
    public void CornerNormalsSmoothTwoHullFacesMeetingAtAShallowAngle()
    {
        // Two triangles sharing edge 0-1, the second tilted 30 degrees - well inside the crease threshold.
        Vector4[] points =
        [
            new(-10, 0, 0, 0),
            new(10, 0, 0, 0),
            new(0, 10, 0, 0),
            new(0, 10 * MathF.Cos(MathF.PI / 6), -10 * MathF.Sin(MathF.PI / 6), 0),
        ];

        FakeShip ship = new(new FakeEliteDraw())
        {
            Rotmat = Matrix4x4.Identity,
            Location = new(0, 0, 1000, 0),
            Model = BuildModel(points, [[0, 1, 2], [0, 1, 3]]),
        };

        // Act
        ship.Draw();

        // Assert: shared corners land halfway between the two faces; each face's own corner keeps its own normal.
        Vector3 halfway = Vector3.Normalize(new(0, MathF.Sin(MathF.PI / 12), MathF.Cos(MathF.PI / 12)));

        AssertVector3AlmostEqual(halfway, ship.CornerNormals[0][0]);
        AssertVector3AlmostEqual(halfway, ship.CornerNormals[0][1]);
        AssertVector3AlmostEqual(new(0, 0, 1), ship.CornerNormals[0][2]);
        AssertVector3AlmostEqual(new(0, 0.5f, MathF.Sqrt(3) / 2), ship.CornerNormals[1][2]);
    }

    [Fact]
    public void DrawShadesEachCornerWhenTheDrawBlendsAcrossAFace()
    {
        // Two triangles sharing edge 0-1, tilted 30 degrees apart, so shared corners smooth and outer ones don't.
        Vector4[] points =
        [
            new(-10, 0, 0, 0),
            new(10, 0, 0, 0),
            new(0, 10, 0, 0),
            new(0, 10 * MathF.Cos(MathF.PI / 6), -10 * MathF.Sin(MathF.PI / 6), 0),
        ];

        // Black has no brightness for a light to vary, so every corner shades alike however it turns.
        ThreeDModel model = BuildModel(points, [[0, 2, 1], [0, 3, 1]]);
        foreach (Face modelFace in model.Faces)
        {
            modelFace.Color = new(255, 255, 255, 255);
        }

        FakeEliteDraw draw = new() { ShadesPerVertex = true };
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = new(0, 0, 1000, 0),
            Model = model,
        };

        // Act
        ship.Draw();

        // Assert: drawn through the per-corner path, not the flat one.
        Assert.Empty(draw.DrawnPolygons);
        (Vector2[] facePoints, float[] _, FastColor[] cornerColors, float _) = draw.DrawnShadedPolygons[0];

        Assert.Equal(facePoints.Length, cornerColors.Length);

        // Corner order 0, 2, 1: the middle corner is turned straight into the light; the shared corners smooth away from it.
        Assert.True(cornerColors[1].R > cornerColors[0].R);
        Assert.Equal(cornerColors[0], cornerColors[2]);
    }

    // A decal left out of smoothing fills flat, keeping the depth bias that settles it against its face.
    [Fact]
    public void DrawKeepsADecalFlatWhileBlendingTheFaceBeneathIt()
    {
        Vector4[] points =
        [
            new(-100, -100, 0, 0),
            new(100, -100, 0, 0),
            new(0, 100, 0, 0),
            new(-10, -10, 0, 0),
            new(10, -10, 0, 0),
            new(0, 10, 0, 0),
        ];

        FakeEliteDraw draw = new() { ShadesPerVertex = true };
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = new(0, 0, 1000, 0),
            Model = BuildModel(points, [[0, 2, 1], [3, 5, 4]]),
        };

        // Act
        ship.Draw();

        // Assert
        Assert.Single(draw.DrawnShadedPolygons);
        (Vector2[] _, float[] decalDepths, FastColor _, float _) = Assert.Single(draw.DrawnPolygons);
        Assert.All(decalDepths, d => Assert.InRange(d, 990f, 999.9f));
    }

    // Close up the hull, decal and line all draw; far enough away that the hull spans only a few pixels, just the hull draws.
    [Theory]
    [InlineData(1000, 3)]
    [InlineData(10000, 1)]
    public void DrawDropsHullDetailAtDistance(float depth, int expectedPolygons)
    {
        // Arrange
        Vector4[] points =
        [
            new(-100, -100, 0, 0),
            new(100, -100, 0, 0),
            new(0, 100, 0, 0),
            new(-10, -10, 0, 0),
            new(10, -10, 0, 0),
            new(0, 10, 0, 0),
            new(-50, -50, 0, 0),
            new(50, -50, 0, 0),
        ];

        FakeEliteDraw draw = new();
        FakeShip ship = new(draw)
        {
            Rotmat = Matrix4x4.Identity,
            Location = new(0, 0, depth, 0),

            // The line takes the plane it lies in from the hull and decal.
            Model = BuildModel(points, [[0, 2, 1], [3, 5, 4], [6, 7]]),
        };

        // Act
        ship.Draw();

        // Assert
        Assert.Equal(expectedPolygons, draw.DrawnPolygons.Count);
    }

    private static ThreeDModel BuildModel(Vector4[] coords, int[][] faceIndices)
    {
        Point[] modelPoints = [.. coords.Select(c => new Point { Coords = c, FaceNormals = [] })];

        return new()
        {
            FaceNormals = [],
            Faces =
            [
                .. faceIndices.Select(indices => new Face
                {
                    Color = default,
                    Points = [.. indices.Select(i => modelPoints[i])],
                    PointIndices = [.. indices],
                }),
            ],
            Lines = [],
            Points = [.. modelPoints],
        };
    }

    private static Vector2 Project(Vector4 localCoords, Vector4 location, FakeEliteDraw draw)
    {
        Vector4 vec = localCoords + location;
        if (vec.Z <= 0)
        {
            vec.Z = 1;
        }

        float x = draw.Layout.ViewportCentre.X + (vec.X * draw.Focus / vec.Z);
        float y = draw.Layout.ViewportCentre.Y - (vec.Y * draw.Focus / vec.Z);
        return new(x, y);
    }

    private static Vector2 ProjectUsingRotmatBasis(Vector4 point, Matrix4x4 rotmat, Vector4 location, FakeEliteDraw draw)
    {
        Vector4 vec = (rotmat.GetRow(0) * point.X) + (rotmat.GetRow(1) * point.Y) + (rotmat.GetRow(2) * point.Z);
        vec += location;

        if (vec.Z <= 0)
        {
            vec.Z = 1;
        }

        float x = draw.Layout.ViewportCentre.X + (vec.X * draw.Focus / vec.Z);
        float y = draw.Layout.ViewportCentre.Y - (vec.Y * draw.Focus / vec.Z);
        return new(x, y);
    }

    private static void AssertVector2AlmostEqual(Vector2 expected, Vector2 actual)
    {
        Assert.InRange(actual.X, expected.X - Tolerance, expected.X + Tolerance);
        Assert.InRange(actual.Y, expected.Y - Tolerance, expected.Y + Tolerance);
    }

    private static void AssertVector3AlmostEqual(Vector3 expected, Vector3 actual)
    {
        Assert.InRange(actual.X, expected.X - Tolerance, expected.X + Tolerance);
        Assert.InRange(actual.Y, expected.Y - Tolerance, expected.Y + Tolerance);
        Assert.InRange(actual.Z, expected.Z - Tolerance, expected.Z + Tolerance);
    }
}
