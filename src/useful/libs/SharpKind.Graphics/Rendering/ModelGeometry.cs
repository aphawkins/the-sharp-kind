// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using System.Runtime.CompilerServices;
using SharpKind.Assets.Models;
using SharpKind.Maths;

namespace SharpKind.Graphics.Rendering;

/// <summary>
/// What a renderer needs to know about a model's shape, derived once from the
/// model itself: which faces sit on which, which way each turns, the normal at
/// each corner, how far the shape reaches, and how bright it is painted.
/// </summary>
/// <remarks>
/// None of this changes while the model is loaded, and every instance drawn
/// from one model gets the same answers - so <see cref="For"/> derives it once
/// per model rather than once per instance. The cache holds no strong
/// reference: the analysis lives exactly as long as the model it describes.
/// </remarks>
public sealed class ModelGeometry
{
    // Models are authored in whole units, so a decal panel meant to sit on a hull face lands exactly; this covers rounding only.
    private const float PlaneTolerance = 0.1f;

    private static readonly ConditionalWeakTable<ThreeDModel, ModelGeometry> s_cache = [];

    private ModelGeometry(ThreeDModel model)
    {
        (int[] roots, Vector3[] normals) = FindFaceRoots(model);
        FaceRoots = roots;
        FaceNormals = normals;
        CornerNormals = BuildCornerNormals(model, roots, normals);
        BoundingRadius = FindBoundingRadius(model);
        FullyLit = LambertShading.FullyLit(model.Faces.Select(f => f.Color));
    }

    /// <summary>
    /// Gets, for each face, the face it sits on: decal faces (cockpit windows,
    /// engine plates) and 2-point detail lines lie exactly in the plane of an
    /// earlier, larger face. They must render over that base face, so they can
    /// share its depth key. Faces on no earlier plane root to themselves.
    /// </summary>
    public IReadOnlyList<int> FaceRoots { get; }

    /// <summary>
    /// Gets each face's model-space unit normal, for the backface cull. A
    /// detail line has no normal of its own, so it takes its root face's -
    /// which is what makes far-side detail cull with the hull it sits on. A
    /// face with neither is <see cref="Vector3.Zero"/>.
    /// </summary>
    public IReadOnlyList<Vector3> FaceNormals { get; }

    /// <summary>
    /// Gets the normal at each corner of each face, for a fill that blends
    /// between them. A face that does not take part in smoothing gets zeroes -
    /// see <see cref="VertexNormals.Build"/>.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<Vector3>> CornerNormals { get; }

    /// <summary>
    /// Gets the distance from the model's origin to its furthest point.
    /// Rotating a model about its origin cannot move it outside this sphere,
    /// so one test against it covers every orientation.
    /// </summary>
    public float BoundingRadius { get; }

    /// <summary>
    /// Gets the brightest channel the model paints anywhere, which is where a
    /// face turned into the light tops out - see
    /// <see cref="LambertShading.UnlitBase"/>.
    /// </summary>
    public byte FullyLit { get; }

    /// <summary>
    /// The analysis of the given model, derived on the first call and reused
    /// on every later one.
    /// </summary>
    /// <param name="model">The model to describe.</param>
    /// <returns>Its geometry.</returns>
    public static ModelGeometry For(ThreeDModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return s_cache.GetValue(model, static m => new ModelGeometry(m));
    }

    private static float FindBoundingRadius(ThreeDModel model)
    {
        float furthest = 0;
        for (int i = 0; i < model.Points.Count; i++)
        {
            furthest = MathF.Max(furthest, model.Points[i].Coords.Length());
        }

        return furthest;
    }

    private static (int[] Roots, Vector3[] Normals) FindFaceRoots(ThreeDModel model)
    {
        int[] roots = new int[model.Faces.Count];
        Vector3[] normals = new Vector3[model.Faces.Count];
        List<(Vector4 Normal, float Offset, int Index)> planes = [];

        for (int i = 0; i < model.Faces.Count; i++)
        {
            roots[i] = i;

            Face face = model.Faces[i];
            foreach ((Vector4 normal, float offset, int index) in planes)
            {
                bool onPlane = true;
                for (int j = 0; j < face.Points.Count && onPlane; j++)
                {
                    onPlane = MathF.Abs(VectorMaths.VectorDotProduct(normal, face.Points[j].Coords) - offset)
                        < PlaneTolerance;
                }

                if (onPlane)
                {
                    roots[i] = roots[index];
                    break;
                }
            }

            if (face.Points.Count >= 3)
            {
                Vector4 edge1 = face.Points[1].Coords - face.Points[0].Coords;
                Vector4 edge2 = face.Points[2].Coords - face.Points[0].Coords;
                Vector3 cross = Vector3.Cross(new(edge1.X, edge1.Y, edge1.Z), new(edge2.X, edge2.Y, edge2.Z));
                if (cross.LengthSquared() > 0)
                {
                    cross = Vector3.Normalize(cross);
                    normals[i] = cross;
                    Vector4 normal = new(cross, 0);
                    planes.Add((normal, VectorMaths.VectorDotProduct(normal, face.Points[0].Coords), i));
                }
            }
            else if (roots[i] != i)
            {
                normals[i] = normals[roots[i]];
            }
        }

        return (roots, normals);
    }

    // Only a self-rooted face takes part in the averaging: counting a decal's normal would weight
    // its plane twice. It still gets corners; they just come out zero.
    private static Vector3[][] BuildCornerNormals(ThreeDModel model, int[] roots, Vector3[] faceNormals)
    {
        IList<int>[] faces = new IList<int>[model.Faces.Count];
        Vector3[] smoothing = new Vector3[model.Faces.Count];

        for (int i = 0; i < faces.Length; i++)
        {
            faces[i] = model.Faces[i].PointIndices;
            smoothing[i] = roots[i] == i ? faceNormals[i] : Vector3.Zero;
        }

        return VertexNormals.Build(faces, smoothing);
    }
}
