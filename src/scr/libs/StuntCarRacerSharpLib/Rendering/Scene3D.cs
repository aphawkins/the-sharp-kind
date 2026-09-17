// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using System.Numerics;
using SharpKind.Graphics;
using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Rendering;

// Following the original software pipeline in 3D Engine.cpp (the parts the Direct3D remake had
// disabled). Works in 'track units' - see SceneCamera.
public sealed class Scene3D
{
    // The original software engine clipped at Z_CLIP_BOUNDARY = 128 to avoid fixed-point overflow,
    // amputating the nearest road. Done in floats here, so the boundary can sit as close as the remake's.
    internal const float NearPlane = 0.5f;

    // Original FOCUS was 512 for a screen width of 640.
    internal const float FocusFactor = 512f / 640f;

    private readonly TrigCoefficients _trig = new();

    private int _cameraX;
    private int _cameraY;
    private int _cameraZ;
    private PerspectiveProjector _projector;

    public void SetView(SceneCamera camera, float screenWidth, float screenHeight)
    {
        ArgumentNullException.ThrowIfNull(camera);

        _cameraX = camera.X;
        _cameraY = camera.Y;
        _cameraZ = camera.Z;

        _trig.CalculateYXZ(camera.XAngle, camera.YAngle, camera.ZAngle);

        _projector = new(screenWidth * FocusFactor, new(screenWidth / 2, screenHeight / 2));
    }

    // Transform a world point (track units, y up) into camera space.
    public Coord3D TransformPoint(int worldX, int worldY, int worldZ)
    {
        long dx = worldX - _cameraX;
        long dy = worldY - _cameraY;
        long dz = worldZ - _cameraZ;

        int x = (int)(((dx * _trig.XX) + (dy * _trig.XY) + (dz * _trig.XZ)) >> Track.LogPrecision);
        int y = (int)(((dx * _trig.YX) + (dy * _trig.YY) + (dz * _trig.YZ)) >> Track.LogPrecision);
        int z = (int)(((dx * _trig.ZX) + (dy * _trig.ZY) + (dz * _trig.ZZ)) >> Track.LogPrecision);

        return new(x, y, z);
    }

    // Perspective projection of a camera-space point (z must be positive).
    public Vector2 ProjectPoint(Coord3D cameraPoint)
        => ProjectPoint(new Vector3(cameraPoint.X, cameraPoint.Y, cameraPoint.Z));

    public Vector2 ProjectPoint(Vector3 cameraPoint) => _projector.Project(cameraPoint);
}
