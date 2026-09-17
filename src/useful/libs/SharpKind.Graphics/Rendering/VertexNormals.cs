// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics.Rendering;

/// <summary>
/// Per-corner normals derived from a model that carries only per-face ones -
/// what a Gouraud fill interpolates between, and the one thing the
/// <c>.obj</c> assets cannot express. A vertex takes the average of the faces
/// meeting there, so a curve drawn as facets shades as a curve.
/// </summary>
/// <remarks>
/// Averaging every adjacent face would round off the edges a model means to
/// have: both games' ships are faceted by design, and a smoothed cube is a
/// blob. So faces only smooth together when they turn through less than
/// <see cref="CreaseAngleDegrees"/> of each other - a crease sharper than that
/// is taken as an edge the artist drew, and each side of it keeps its own face
/// normal. This is per face *and* corner rather than per vertex for exactly
/// that reason: the vertex on a cube's corner has three different answers, one
/// for each face it belongs to.
/// </remarks>
public static class VertexNormals
{
    /// <summary>
    /// Gets how far two faces may turn through and still smooth together. Chosen
    /// to keep a cube's 90-degree corners hard while letting the many-sided
    /// hulls (which step through far smaller angles) round off.
    /// </summary>
    public static float CreaseAngleDegrees => 45f;

    private static float CreaseCosine { get; } = MathF.Cos(CreaseAngleDegrees * MathF.PI / 180f);

    /// <summary>
    /// The normal at each corner of each face.
    /// </summary>
    /// <param name="faces">
    /// Each face's point indices, into a shared point list.
    /// </param>
    /// <param name="faceNormals">
    /// Each face's own unit normal, parallel to <paramref name="faces"/>.
    /// <see cref="Vector3.Zero"/> for a face that takes no part in smoothing -
    /// one with no direction of its own (collinear points), or one lying in
    /// another's plane (a decal, a detail line), whose normal would otherwise
    /// count twice at every corner it touches.
    /// </param>
    /// <returns>
    /// For each face, one normal per corner. A face whose own normal is
    /// <see cref="Vector3.Zero"/> gets zeroes, having no direction to smooth.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="faceNormals"/> does not have one entry per face.
    /// </exception>
    public static Vector3[][] Build(IReadOnlyList<IList<int>> faces, IReadOnlyList<Vector3> faceNormals)
    {
        ArgumentNullException.ThrowIfNull(faces);
        ArgumentNullException.ThrowIfNull(faceNormals);

        if (faces.Count != faceNormals.Count)
        {
            throw new ArgumentException("A normal is needed for every face.", nameof(faceNormals));
        }

        List<int>[] facesAtPoint = FacesAtPoint(faces, faceNormals);
        Vector3[][] normals = new Vector3[faces.Count][];

        for (int i = 0; i < faces.Count; i++)
        {
            IList<int> face = faces[i];
            normals[i] = new Vector3[face.Count];

            Vector3 faceNormal = faceNormals[i];
            if (faceNormal == Vector3.Zero)
            {
                continue;
            }

            for (int corner = 0; corner < face.Count; corner++)
            {
                normals[i][corner] = SmoothedNormal(faceNormal, facesAtPoint[face[corner]], faceNormals);
            }
        }

        return normals;
    }

    // Which faces meet at each point, counting only those that smooth. Sized from the indices, not a caller-supplied point count.
    private static List<int>[] FacesAtPoint(IReadOnlyList<IList<int>> faces, IReadOnlyList<Vector3> faceNormals)
    {
        int pointCount = 0;
        for (int i = 0; i < faces.Count; i++)
        {
            foreach (int index in faces[i])
            {
                pointCount = Math.Max(pointCount, index + 1);
            }
        }

        List<int>[] facesAtPoint = new List<int>[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            facesAtPoint[i] = [];
        }

        for (int i = 0; i < faces.Count; i++)
        {
            if (faceNormals[i] == Vector3.Zero)
            {
                continue;
            }

            foreach (int index in faces[i])
            {
                facesAtPoint[index].Add(i);
            }
        }

        return facesAtPoint;
    }

    // The face itself is in the list, so the sum is empty only if the model's normals cancel exactly (a real degenerate case); then the face keeps its own.
    private static Vector3 SmoothedNormal(Vector3 faceNormal, List<int> facesHere, IReadOnlyList<Vector3> faceNormals)
    {
        Vector3 sum = Vector3.Zero;

        foreach (int i in facesHere)
        {
            Vector3 neighbour = faceNormals[i];
            if (Vector3.Dot(faceNormal, neighbour) >= CreaseCosine)
            {
                sum += neighbour;
            }
        }

        return sum.LengthSquared() > 0 ? Vector3.Normalize(sum) : faceNormal;
    }
}
