// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using System.Numerics;
using SharpKind.Assets.Models;
using SharpKind.Graphics.Rendering;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Rendering;

// Original Car.cpp's CreateCarInVB: four flat wheel quads and a wedge-shaped body, loaded from
// car.obj and coloured via CarPalette, oriented onto a frame derived from the car's wheel positions.
public sealed class CarMesh
{
    // Original VISIBLE_CAR dimensions. car.obj carries these as +/- half width/length and +/- a
    // quarter height around its origin, so anything positioning the mesh has to reckon with them.
    internal const int CarWidth = 162;

    internal const int CarLength = 256;

    internal const int CarHeight = 162;

    private readonly ThreeDModel _model;

    // How far the mesh's lowest point sits below its own origin, so Append
    // can raise the mesh to sit exactly on the wheel-corner frame.
    private readonly float _bottomOffset;

    public CarMesh(ScrPalette palette)
        : this(Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "car.obj"), palette)
    {
    }

    internal CarMesh(string modelPath, ScrPalette palette)
    {
        _model = ModelReader.Read(modelPath, CarPalette.Colours(palette));
        _bottomOffset = -_model.Points.Min(p => p.Coords.Y);
    }

    // Appends the car mesh polygons, oriented on the given wheel-corner
    // positions (world track units); the mesh bottom sits at corner level.
    public void Append(
        ICollection<WorldPolygon> polygons,
        Coord3D rearLeft,
        Coord3D rearRight,
        Coord3D frontLeft,
        Coord3D frontRight)
    {
        ArgumentNullException.ThrowIfNull(polygons);

        // orientation frame from the wheel corners
        (double rightX, double rightY, double rightZ) = Normalize(
            rearRight.X - rearLeft.X,
            rearRight.Y - rearLeft.Y,
            rearRight.Z - rearLeft.Z);

        double frontX = (frontLeft.X + frontRight.X) / 2.0;
        double frontY = (frontLeft.Y + frontRight.Y) / 2.0;
        double frontZ = (frontLeft.Z + frontRight.Z) / 2.0;
        double rearX = (rearLeft.X + rearRight.X) / 2.0;
        double rearY = (rearLeft.Y + rearRight.Y) / 2.0;
        double rearZ = (rearLeft.Z + rearRight.Z) / 2.0;

        (double forwardX, double forwardY, double forwardZ) = Normalize(frontX - rearX, frontY - rearY, frontZ - rearZ);

        // up = forward x right (positive y when the car is level)
        (double vertX, double vertY, double vertZ) = Normalize(
            (forwardY * rightZ) - (forwardZ * rightY),
            (forwardZ * rightX) - (forwardX * rightZ),
            (forwardX * rightY) - (forwardY * rightX));

        // origin at the centre of the corners, raised so the mesh bottom
        // sits at corner level
        double originX = ((frontX + rearX) / 2) + (vertX * _bottomOffset);
        double originY = ((frontY + rearY) / 2) + (vertY * _bottomOffset);
        double originZ = ((frontZ + rearZ) / 2) + (vertZ * _bottomOffset);

        // The frame's rows are where the model's own x, y and z axes end up:
        // the model is authored x = right, y = up, z = forward.
        Matrix4x4 frame = default;
        frame.M11 = (float)rightX;
        frame.M12 = (float)rightY;
        frame.M13 = (float)rightZ;
        frame.M21 = (float)vertX;
        frame.M22 = (float)vertY;
        frame.M23 = (float)vertZ;
        frame.M31 = (float)forwardX;
        frame.M32 = (float)forwardY;
        frame.M33 = (float)forwardZ;
        frame.M44 = 1;

        Span<Vector3> world = stackalloc Vector3[_model.Points.Count];
        MeshTransform.TransformPoints(
            _model,
            frame,
            new((float)originX, (float)originY, (float)originZ),
            world);

        Span<Vector3> facePoints = stackalloc Vector3[MeshTransform.MaxFacePoints(_model)];
        for (int i = 0; i < _model.Faces.Count; i++)
        {
            int count = MeshTransform.FacePoints(_model, i, world, facePoints);

            // The track works in whole units, so the mesh lands on them here -
            // the same rounding the hand-rolled transform did.
            Coord3D[] points = new Coord3D[count];
            for (int j = 0; j < count; j++)
            {
                points[j] = new((int)facePoints[j].X, (int)facePoints[j].Y, (int)facePoints[j].Z);
            }

            polygons.Add(new(points, _model.Faces[i].Color));
        }
    }

    private static (double X, double Y, double Z) Normalize(double x, double y, double z)
    {
        double length = Math.Sqrt((x * x) + (y * y) + (z * z));
        if (length < 1e-9)
        {
            return (0, 0, 0);
        }

        return (x / length, y / length, z / length);
    }
}
