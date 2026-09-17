// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Graphics;
using SharpKind;
using SharpKind.Assets.Models;
using SharpKind.Graphics;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib.Ships;

internal class ShipBase : IShip
{
    // Large enough that Location's contribution is negligible, so the result approximates the
    // on-screen vanishing point of the local direction rather than a specific 3D position.
    private const float FarAimDistance = 1_000_000f;
    private const int LaserAimSpread = 24;

    // Effectively the camera plane; keeps the perspective divide out of the sign flip.
    private const float NearPlane = 1f;

    // Ship faces are small polygons; anything larger falls back to the heap.
    private const int StackFacePoints = 16;

    // The frustum's far plane, matching the range Space.cs already removes a ship at.
    private const float FarPlane = 57344f;

    // Pulls a decal nearer than the hull face it sits on, so per-vertex depth doesn't tie
    // exactly and speckle through under interpolation. 1% covers the interpolation error
    // without punching through a face genuinely in front.
    private const float DecalDepthBias = 0.99f;

    // Screen radius below which hull panels/lines stop drawing - about six pixels of a decal,
    // where it stops reading as a shape. Derived from projected size, not a fixed range.
    private const float DetailScreenRadius = 16f;

    private readonly IEliteDraw _draw;
    private readonly IRandomSource _rng;

    // Derived whenever the model is set, so the two always agree.
    private ModelGeometry _geometry = ModelGeometry.For(ModelReader.None);

    // Reused across frames so drawing a ship doesn't allocate; grown to the model's point count on first use.
    private Vector4[] _pointList = [];
    private Vector3[] _cameraList = [];

    internal ShipBase(IEliteDraw draw)
    {
        _draw = draw;
        _rng = draw.Jitter;
        Model = ModelReader.None;
    }

    private ShipBase(ShipBase other)
    {
        _draw = other._draw;
        _rng = other._rng;
        Model = other.Model;
    }

    public float Acceleration { get; set; }

    public float Bounty { get; set; }

    public int Bravery { get; set; }

    public int Energy { get; set; }

    public int EnergyMax { get; set; }

    public float ExpDelta { get; set; }

    public ShipProperties Flags { get; set; } = ShipProperties.None;

    public int LaserFront { get; set; }

    public int LaserStrength { get; set; }

    public Vector4 Location { get; set; }

    public int LootMax { get; set; }

    public float MinDistance { get; set; }

    public int Missiles { get; set; }

    public int MissilesMax { get; set; }

    public string Name { get; set; } = string.Empty;

    public Matrix4x4 Rotmat { get; set; }

    public float RotX { get; set; }

    public float RotZ { get; set; }

    public string? ScoopedType { get; set; }

    public float Size { get; set; }

    public IObject? Target { get; set; }

    public string Id { get; set; } = ObjectIds.None;

    public ShipTraits Traits { get; set; }

    public int VanishPoint { get; set; }

    public float Velocity { get; set; }

    public float VelocityMax { get; set; }

    public ThreeDModel Model
    {
        get;

        set
        {
            field = value;
            _geometry = ModelGeometry.For(value);
        }
    }

    // Gets the model's normal at each corner of each face. A Gouraud fill
    // interpolates between these; a flat one has no use for them.
    internal IReadOnlyList<IReadOnlyList<Vector3>> CornerNormals => _geometry.CornerNormals;

    public IObject Clone()
    {
        ShipBase ship = new(this);
        this.CopyTo(ship);
        return ship;
    }

    /// <summary>
    /// Hacked version of the draw ship routine to display ships...
    /// This needs a lot of tidying...
    /// caveat: it is a work in progress.
    /// Check for hidden surface supplied by T.Harte.
    /// </summary>
    public virtual void Draw()
    {
        if (!IsWithinView())
        {
            return;
        }

        if (_pointList.Length < Model.Points.Count)
        {
            _pointList = new Vector4[Model.Points.Count];
            _cameraList = new Vector3[Model.Points.Count];
        }

        Vector4[] pointList = _pointList;

        TransformModelPoints(Rotmat, pointList);
        DrawModelFaces(pointList);
        DrawLasers(pointList);
    }

    // Bounding sphere from the model's geometry, not the hand-authored Size (Combat's collision
    // radius squared, which need not agree with it). Conservative: a ship with any part on
    // screen always draws.
    private bool IsWithinView()
    {
        ViewFrustum frustum = ViewFrustum.FromViewport(
            _draw.Projector,
            _draw.Layout.ViewportLeft,
            _draw.Layout.ViewportTop,
            _draw.Layout.ViewportWidth,
            _draw.Layout.ViewportHeight,
            NearPlane,
            FarPlane);

        return frustum.Intersects(new(Location.X, Location.Y, Location.Z), _geometry.BoundingRadius);
    }

    // Placing is common to any renderer of a model, so the library does it; the projection is this game's own.
    private void TransformModelPoints(Matrix4x4 transform, Vector4[] pointList)
    {
        MeshTransform.TransformPoints(
            Model,
            transform,
            new(Location.X, Location.Y, Location.Z),
            _cameraList);

        for (int i = 0; i < Model.Points.Count; i++)
        {
            Vector3 camera = _cameraList[i];
            pointList[i] = ProjectPoint(new(camera, 1));
        }
    }

    private Vector4 ProjectPoint(Vector4 localCoords, Matrix4x4 transform)
        => ProjectPoint(Vector4.Transform(localCoords, transform) + Location);

    // Keeps the original's depth clamp for laser aim; faces themselves are culled and clipped properly instead.
    private Vector4 ProjectPoint(Vector4 cameraCoords)
    {
        Vector4 vec = cameraCoords;

        if (vec.Z <= 0)
        {
            vec.Z = 1;
        }

        Vector2 screen = _draw.Projector.Project(vec.X, vec.Y, vec.Z);
        vec.X = screen.X;
        vec.Y = screen.Y;

        return vec;
    }

    private Vector2 ProjectCameraPoint(Vector3 cameraPoint) => _draw.Projector.Project(cameraPoint);

    private void DrawModelFaces(Vector4[] pointList)
    {
        int maxPoints = MeshTransform.MaxFacePoints(Model);

        Span<Vector3> face = maxPoints <= StackFacePoints ? stackalloc Vector3[StackFacePoints] : new Vector3[maxPoints];
        Span<Vector3> clipped = maxPoints <= StackFacePoints ? stackalloc Vector3[StackFacePoints + 1] : new Vector3[maxPoints + 1];
        Span<FastColor> corners = maxPoints <= StackFacePoints ? stackalloc FastColor[StackFacePoints] : new FastColor[maxPoints];
        Span<FastColor> clippedCorners = maxPoints <= StackFacePoints
            ? stackalloc FastColor[StackFacePoints + 1]
            : new FastColor[maxPoints + 1];

        bool showsDetail = ShowsDetail();

        for (int i = 0; i < Model.Faces.Count; i++)
        {
            if (!showsDetail && IsDetail(i))
            {
                continue;
            }

            if (!IsFacingCamera(i, out Vector3 cameraNormal))
            {
                continue;
            }

            if (ShadesCorners(i))
            {
                DrawShadedFace(i, pointList, face, clipped, corners, clippedCorners);
                continue;
            }

            DrawFlatFace(i, cameraNormal, pointList, face, clipped);
        }
    }

    // Bounding sphere projected at the ship's own depth; a ship around or behind the camera plane keeps its detail.
    private bool ShowsDetail()
        => _draw.Projector.Focus * _geometry.BoundingRadius / MathF.Max(Location.Z, NearPlane)
            >= DetailScreenRadius;

    // A 2-point line or a decal face lying in an earlier face's plane; both root to a face other than themselves.
    private bool IsDetail(int faceIndex)
        => Model.Faces[faceIndex].Points.Count < 3 || _geometry.FaceRoots[faceIndex] != faceIndex;

    // One face as a single colour: the model's own, as this rendition's lighting leaves it.
    private void DrawFlatFace(
        int faceIndex,
        Vector3 cameraNormal,
        Vector4[] pointList,
        in Span<Vector3> cameraPoints,
        in Span<Vector3> clipped)
    {
        float bias = _geometry.FaceRoots[faceIndex] == faceIndex ? 1f : DecalDepthBias;
        Vector2[]? poly_list = BuildFacePolygon(
            faceIndex,
            pointList,
            cameraPoints,
            clipped,
            bias,
            out float[] depths);

        if (poly_list != null)
        {
            FastColor color = _draw.ShadeFace(Model.Faces[faceIndex].Color, cameraNormal, _geometry.FullyLit);
            _draw.DrawPolygonFilled(poly_list, depths, color, FaceMeanZ(_geometry.FaceRoots[faceIndex], pointList));
        }
    }

    // One face shaded at each corner, for the fill to blend between.
    private void DrawShadedFace(
        int faceIndex,
        Vector4[] pointList,
        in Span<Vector3> cameraPoints,
        in Span<Vector3> clipped,
        in Span<FastColor> cornerColours,
        in Span<FastColor> clippedColours)
    {
        Vector2[]? polygon = BuildShadedFacePolygon(
            faceIndex,
            cameraPoints,
            clipped,
            cornerColours,
            clippedColours,
            out float[] depths,
            out FastColor[] colours);

        if (polygon != null)
        {
            _draw.DrawPolygonFilled(polygon, depths, colours, FaceMeanZ(faceIndex, pointList));
        }
    }

    // A decal or detail line has zero corner normals (left out of smoothing) and fills flat.
    private bool ShadesCorners(int faceIndex)
        => _draw.ShadesPerVertex
            && CornerNormals[faceIndex].Count >= 3
            && CornerNormals[faceIndex][0] != Vector3.Zero;

    // Backface cull in camera space, not on the projected outline, to keep it off the near-plane
    // depth clamp which produces meaningless X/Y for a face straddling the camera plane.
    // cameraNormal is handed back so lighting need not repeat the rotation; it is Vector3.Zero
    // for a normal-less detail line, which takes the model's flat colour instead.
    private bool IsFacingCamera(int faceIndex, out Vector3 cameraNormal)
    {
        Face face = Model.Faces[faceIndex];
        Vector3 surfacePoint = _cameraList[face.PointIndices[0]];
        Vector3 normal = _geometry.FaceNormals[faceIndex];

        if (normal == Vector3.Zero)
        {
            cameraNormal = Vector3.Zero;
            return AnySharedVertexNormalFacesCamera(face, surfacePoint);
        }

        cameraNormal = RotateToCamera(normal);
        return Vector3.Dot(cameraNormal, surfacePoint) <= 0;
    }

    // Lighting wants the rotated vector itself, not just which side of the camera it falls.
    private Vector3 RotateToCamera(Vector3 normal)
    {
        Vector4 rotated = Vector4.Transform(new Vector4(normal, 0), Rotmat);
        return new(rotated.X, rotated.Y, rotated.Z);
    }

    // True when a model-space normal, rotated into view, turns towards the
    // camera at the given camera-space point on the surface.
    private bool FacesCamera(Vector3 normal, Vector3 surfacePoint)
        => Vector3.Dot(RotateToCamera(normal), surfacePoint) <= 0;

    // A rootless detail line has no normal of its own, so it's visible when any face shared by
    // all its vertices is - matching the original's rule. A line sharing none draws unculled.
    private bool AnySharedVertexNormalFacesCamera(Face face, Vector3 surfacePoint)
    {
        // Identity, not equality: two distinct faces can carry numerically equal normals.
        static bool SharesNormal(Point point, FaceNormal normal)
        {
            foreach (FaceNormal candidate in point.FaceNormals)
            {
                if (ReferenceEquals(candidate, normal))
                {
                    return true;
                }
            }

            return false;
        }

        bool anyShared = false;

        foreach (FaceNormal candidate in face.Points[0].FaceNormals)
        {
            bool sharedByAll = true;
            for (int j = 1; j < face.Points.Count && sharedByAll; j++)
            {
                sharedByAll = SharesNormal(face.Points[j], candidate);
            }

            if (!sharedByAll)
            {
                continue;
            }

            anyShared = true;
            Vector4 direction = candidate.Direction;
            if (FacesCamera(new(direction.X, direction.Y, direction.Z), surfacePoint))
            {
                return true;
            }
        }

        return !anyShared;
    }

    // A single vertex is visible when any face it belongs to is. A vertex
    // the model records no normals for has nothing to cull against.
    private bool IsPointFacingCamera(int pointIndex)
    {
        Vector3 surfacePoint = _cameraList[pointIndex];
        bool any = false;

        foreach (FaceNormal normal in Model.Points[pointIndex].FaceNormals)
        {
            any = true;
            Vector4 direction = normal.Direction;
            if (FacesCamera(new(direction.X, direction.Y, direction.Z), surfacePoint))
            {
                return true;
            }
        }

        return !any;
    }

    // Screen outline clipped to the near plane, with per-point camera-space depth; null if
    // entirely behind it. depthBias scales those depths to settle a decal against its face.
    private Vector2[]? BuildFacePolygon(
        int faceIndex,
        Vector4[] pointList,
        in Span<Vector3> cameraPoints,
        in Span<Vector3> clipped,
        float depthBias,
        out float[] depths)
    {
        Face face = Model.Faces[faceIndex];
        int numPoints = face.Points.Count;

        // Not a polygon - the cyclic clipper would walk its single edge twice - so keep the clamped projection.
        if (numPoints < 3)
        {
            Vector2[] line = new Vector2[numPoints];
            depths = new float[numPoints];
            for (int j = 0; j < numPoints; j++)
            {
                int index = face.PointIndices[j];
                line[j] = new(pointList[index].X, pointList[index].Y);
                depths[j] = pointList[index].Z * depthBias;
            }

            return line;
        }

        MeshTransform.FacePoints(Model, faceIndex, _cameraList, cameraPoints);

        int count = NearPlaneClip.Clip(cameraPoints[..numPoints], NearPlane, clipped);
        if (count < 3)
        {
            depths = [];
            return null;
        }

        Vector2[] polygon = new Vector2[count];
        depths = new float[count];
        for (int j = 0; j < count; j++)
        {
            polygon[j] = ProjectCameraPoint(clipped[j]);
            depths[j] = clipped[j].Z * depthBias;
        }

        return polygon;
    }

    // As BuildFacePolygon, carrying corner colours through the clip. No depth bias: only a
    // self-rooted face gets here, and nothing sits in its plane to tie with.
    private Vector2[]? BuildShadedFacePolygon(
        int faceIndex,
        in Span<Vector3> cameraPoints,
        in Span<Vector3> clipped,
        in Span<FastColor> cornerColours,
        in Span<FastColor> clippedColours,
        out float[] depths,
        out FastColor[] colours)
    {
        Face face = Model.Faces[faceIndex];
        IReadOnlyList<Vector3> cornerNormals = CornerNormals[faceIndex];
        int numPoints = face.Points.Count;

        MeshTransform.FacePoints(Model, faceIndex, _cameraList, cameraPoints);

        for (int j = 0; j < numPoints; j++)
        {
            cornerColours[j] = _draw.ShadeVertex(face.Color, RotateToCamera(cornerNormals[j]), _geometry.FullyLit);
        }

        int count = NearPlaneClip.Clip(
            cameraPoints[..numPoints],
            cornerColours[..numPoints],
            NearPlane,
            clipped,
            clippedColours);

        if (count < 3)
        {
            depths = [];
            colours = [];
            return null;
        }

        Vector2[] polygon = new Vector2[count];
        depths = new float[count];
        colours = new FastColor[count];
        for (int j = 0; j < count; j++)
        {
            polygon[j] = ProjectCameraPoint(clipped[j]);
            depths[j] = clipped[j].Z;
            colours[j] = clippedColours[j];
        }

        return polygon;
    }

    // Decals and detail lines use their root face's key so they tie exactly with the surface they sit on.
    private float FaceMeanZ(int faceIndex, Vector4[] pointList)
    {
        Face face = Model.Faces[faceIndex];
        float z = 0;
        for (int j = 0; j < face.Points.Count; j++)
        {
            z += pointList[face.PointIndices[j]].Z;
        }

        return z / face.Points.Count;
    }

    private void DrawLasers(Vector4[] pointList)
    {
        if (!Flags.HasFlag(ShipProperties.Firing))
        {
            return;
        }

        int lasv = LaserFront;

        // Without this a ship firing away from us draws its bolt through its own hull -
        // hidden by the depth test in z-buffered mode, but wireframe has no depth buffer.
        if (!IsPointFacingCamera(lasv))
        {
            return;
        }

        // A Viper's beam is the colour a Viper is on the scanner: both come from one rendition definition.
        FastColor color = _draw.Ships.For(Id == ObjectIds.Viper ? ShipClass.Police : ShipClass.Default);

        Vector2 mount = new(pointList[lasv].X, pointList[lasv].Y);

        // Aim along the ship's real firing direction, projected a long way out to approximate
        // where it vanishes on screen, plus a small random spread so shots aren't identical.
        Vector4 aimPoint = ProjectPoint(Model.Points[lasv].Coords * FarAimDistance, Rotmat);
        float aimX = aimPoint.X + _rng.Random(-LaserAimSpread, LaserAimSpread);
        float aimY = aimPoint.Y + _rng.Random(-LaserAimSpread, LaserAimSpread);
        Vector2 direction = new Vector2(aimX, aimY) - mount;

        Vector2 endPoint = ProjectToViewBoundary(mount, direction);

        // The far end has no camera-space depth, so the whole line tests at the mount's depth,
        // biased nearer like a decal so the hull it springs from cannot swallow it.
        float mountZ = pointList[lasv].Z * DecalDepthBias;
        _draw.DrawPolygonFilled([mount, endPoint], [mountZ, mountZ], color, mountZ);
    }

    // Finds where a ray from origin along direction leaves the view rectangle,
    // so the laser is clipped to the actual viewport rather than a hardcoded
    // screen size.
    private Vector2 ProjectToViewBoundary(Vector2 origin, Vector2 direction)
    {
        float exitDistance = float.PositiveInfinity;

        if (direction.X > 0)
        {
            exitDistance = MathF.Min(exitDistance, (_draw.Layout.ViewportRight - origin.X) / direction.X);
        }
        else if (direction.X < 0)
        {
            exitDistance = MathF.Min(exitDistance, (_draw.Layout.ViewportLeft - origin.X) / direction.X);
        }

        if (direction.Y > 0)
        {
            exitDistance = MathF.Min(exitDistance, (_draw.Layout.ViewportBottom - origin.Y) / direction.Y);
        }
        else if (direction.Y < 0)
        {
            exitDistance = MathF.Min(exitDistance, (_draw.Layout.ViewportTop - origin.Y) / direction.Y);
        }

        return !float.IsFinite(exitDistance) || exitDistance <= 0
            ? origin
            : origin + (direction * exitDistance);
    }
}
