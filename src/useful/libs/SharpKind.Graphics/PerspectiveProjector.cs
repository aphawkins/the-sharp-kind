// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics;

// Screen = viewport centre + focus * x / z, y negated since camera space has y up and the screen has y down.
// Both games use exactly this form; anything scaled by focal length but not projected (e.g. Elite's world
// radii) stays with the game that needs it.
public readonly record struct PerspectiveProjector(float Focus, Vector2 Centre)
{
    // z must be positive: points at or behind the camera plane project to
    // garbage, so clip against the near plane (see NearPlaneClip) first.
    public Vector2 Project(Vector3 cameraPoint) => Project(cameraPoint.X, cameraPoint.Y, cameraPoint.Z);

    public Vector2 Project(float x, float y, float z) => new(
        Centre.X + (Focus * x / z),
        Centre.Y - (Focus * y / z));
}
