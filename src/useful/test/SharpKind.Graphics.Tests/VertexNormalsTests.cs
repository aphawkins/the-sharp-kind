// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics.Tests;

public class VertexNormalsTests
{
    private const float Tolerance = 0.0001f;

    // Two triangles sharing the edge 1-2, so points 1 and 2 are the only ones
    // where two faces meet.
    private static readonly IList<int>[] s_sharedEdge = [[0, 1, 2], [1, 2, 3]];

    [Fact]
    public void EachFaceGetsOneNormalPerCorner()
    {
        Vector3[][] normals = VertexNormals.Build([[0, 1, 2], [1, 2, 3, 4]], [UnitAt(0), UnitAt(0)]);

        Assert.Equal(2, normals.Length);
        Assert.Equal(3, normals[0].Length);
        Assert.Equal(4, normals[1].Length);
    }

    // The angle a cube's faces turn through. Smoothing here would round off
    // the corners the model means to have.
    [Fact]
    public void FacesMeetingAtASharpCreaseEachKeepTheirOwnNormal()
    {
        Vector3 a = UnitAt(0);
        Vector3 b = UnitAt(90);

        Vector3[][] normals = VertexNormals.Build(s_sharedEdge, [a, b]);

        Assert.All(normals[0], n => AssertAlmostEqual(a, n));
        Assert.All(normals[1], n => AssertAlmostEqual(b, n));
    }

    [Fact]
    public void FacesMeetingAtAShallowAngleSmoothTogetherOnTheirSharedPoints()
    {
        Vector3 a = UnitAt(0);
        Vector3 b = UnitAt(30);
        Vector3 averaged = Vector3.Normalize(a + b);

        Vector3[][] normals = VertexNormals.Build(s_sharedEdge, [a, b]);

        // Point 0 belongs to the first face alone, point 3 to the second.
        AssertAlmostEqual(a, normals[0][0]);
        AssertAlmostEqual(b, normals[1][2]);

        // Points 1 and 2 are shared, and both faces see the same answer there.
        AssertAlmostEqual(averaged, normals[0][1]);
        AssertAlmostEqual(averaged, normals[0][2]);
        AssertAlmostEqual(averaged, normals[1][0]);
        AssertAlmostEqual(averaged, normals[1][1]);
    }

    [Theory]
    [InlineData(44f, true)]
    [InlineData(46f, false)]
    public void TheCreaseThresholdSitsAtFortyFiveDegrees(float degrees, bool expectSmoothed)
    {
        Vector3 a = UnitAt(0);
        Vector3 b = UnitAt(degrees);

        Vector3[][] normals = VertexNormals.Build(s_sharedEdge, [a, b]);

        Assert.Equal(expectSmoothed, normals[0][1] != a);
    }

    // Averaging a decal's normal in would count its hull face's plane twice and pull every corner it touches back towards flat.
    [Fact]
    public void AFaceWithNoNormalNeitherSmoothsNorIsSmoothed()
    {
        Vector3 a = UnitAt(0);
        Vector3 b = UnitAt(30);

        // A third face on the shared edge, in the first face's plane.
        IList<int>[] faces = [[0, 1, 2], [1, 2, 3], [1, 2]];
        Vector3[][] normals = VertexNormals.Build(faces, [a, b, Vector3.Zero]);

        AssertAlmostEqual(Vector3.Normalize(a + b), normals[0][1]);
        Assert.All(normals[2], n => Assert.Equal(Vector3.Zero, n));
    }

    // The models contain faces whose points are collinear, which have no
    // direction to light and none to smooth either.
    [Fact]
    public void AFaceWhoseNormalsCancelKeepsItsOwn()
    {
        Vector3 a = UnitAt(0);

        Vector3[][] normals = VertexNormals.Build(s_sharedEdge, [a, -a]);

        Assert.All(normals[0], n => AssertAlmostEqual(a, n));
    }

    [Fact]
    public void AModelWithNoFacesHasNoNormals() => Assert.Empty(VertexNormals.Build([], []));

    [Fact]
    public void EveryFaceNeedsANormal()
        => Assert.Throws<ArgumentException>(() => VertexNormals.Build(s_sharedEdge, [UnitAt(0)]));

    // A unit vector in the XZ plane, turned the given angle off -Z.
    private static Vector3 UnitAt(float degrees)
    {
        float radians = degrees * MathF.PI / 180f;
        return new(MathF.Sin(radians), 0, -MathF.Cos(radians));
    }

    private static void AssertAlmostEqual(Vector3 expected, Vector3 actual)
    {
        Assert.InRange(actual.X, expected.X - Tolerance, expected.X + Tolerance);
        Assert.InRange(actual.Y, expected.Y - Tolerance, expected.Y + Tolerance);
        Assert.InRange(actual.Z, expected.Z - Tolerance, expected.Z + Tolerance);
    }
}
