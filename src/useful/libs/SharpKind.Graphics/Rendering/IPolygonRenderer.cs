// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics.Rendering;

// Isolated so algorithms (painter's, z-buffer, wireframe) can be swapped by DI registration instead of editing the caller.
public interface IPolygonRenderer
{
    // depths is the camera-space depth per point, parallel to points, for a per-pixel depth test.
    // z is a single whole-polygon key for strategies that order polygons rather than pixels.
    public void Submit(Vector2[] points, float[] depths, FastColor color, float z);

    // As Submit, carrying a quantiser the fill has to ask per pixel - which
    // only a dither does. Null when the caller already resolved the colour.
    public void Submit(Vector2[] points, float[] depths, FastColor color, float z, IColourQuantiser? dither);

    // As Submit, with a colour per point (Gouraud shading): the quantiser is asked per pixel since
    // an interpolated colour can't be resolved once. A strategy that can't blend (painter's,
    // wireframe) stands the colours down to one, so callers submit per-vertex colours unconditionally.
    public void Submit(Vector2[] points, float[] depths, FastColor[] colours, float z, IColourQuantiser? quantiser);

    public void StartFrame();

    public void EndFrame();
}
