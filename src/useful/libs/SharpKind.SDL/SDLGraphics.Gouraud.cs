// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Graphics.Rendering;

namespace SharpKind.SDL;

// Gouraud fill for the CPU depth layer. Its own file for the repo's per-file size limit, and
// mirrors SoftwareGraphics.Gouraud.cs as the rest of the layer mirrors SoftwareGraphics.
public sealed unsafe partial class SDLGraphics
{
    public void DrawPolygonFilledDepth(
        Vector2[] points,
        float[] depths,
        FastColor[] vertexColors,
        IColourQuantiser? quantiser)
    {
        if (_isDisposed
            || points == null
            || depths == null
            || vertexColors == null
            || depths.Length < points.Length
            || vertexColors.Length < points.Length
            || _depthLayer == null)
        {
            return;
        }

        for (int i = 1; i < points.Length - 1; i++)
        {
            DrawTriangleFilledDepthGouraudToLayer(
                points[0],
                points[i],
                points[i + 1],
                depths[0],
                depths[i],
                depths[i + 1],
                vertexColors[0],
                vertexColors[i],
                vertexColors[i + 1],
                quantiser);
        }

        _depthLayerDirty = true;
    }

    // Mirrors SoftwareGraphics.DrawTriangleFilledDepthGouraud, as the flat pair above mirror their software counterparts.
    private void DrawTriangleFilledDepthGouraudToLayer(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        float za,
        float zb,
        float zc,
        in FastColor colorA,
        in FastColor colorB,
        in FastColor colorC,
        IColourQuantiser? quantiser)
    {
        if (za <= 0 || zb <= 0 || zc <= 0)
        {
            return;
        }

        FastColor ca = colorA;
        FastColor cb = colorB;
        FastColor cc = colorC;

        if (b.Y < a.Y)
        {
            (a, b, za, zb, ca, cb) = (b, a, zb, za, cb, ca);
        }

        if (c.Y < a.Y)
        {
            (a, c, za, zc, ca, cc) = (c, a, zc, za, cc, ca);
        }

        if (c.Y < b.Y)
        {
            (b, c, zb, zc, cb, cc) = (c, b, zc, zb, cc, cb);
        }

        float ia = 1f / za;
        float ib = 1f / zb;
        float ic = 1f / zc;

        int firstY = Math.Max((int)MathF.Ceiling(a.Y), 0);
        int lastY = Math.Min((int)MathF.Floor(c.Y), (int)ScreenHeight - 1);

        for (int y = firstY; y <= lastY; y++)
        {
            float t0 = EdgeT(a, c, y);
            float x0 = a.X + ((c.X - a.X) * t0);
            float i0 = ia + ((ic - ia) * t0);
            FastColor colour0 = VertexColours.Lerp(ca, cc, t0);

            float x1;
            float i1;
            FastColor colour1;
            if (y < b.Y)
            {
                float t1 = EdgeT(a, b, y);
                x1 = a.X + ((b.X - a.X) * t1);
                i1 = ia + ((ib - ia) * t1);
                colour1 = VertexColours.Lerp(ca, cb, t1);
            }
            else
            {
                float t1 = EdgeT(b, c, y);
                x1 = b.X + ((c.X - b.X) * t1);
                i1 = ib + ((ic - ib) * t1);
                colour1 = VertexColours.Lerp(cb, cc, t1);
            }

            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
                (i0, i1) = (i1, i0);
                (colour0, colour1) = (colour1, colour0);
            }

            DrawSpanFilledDepthGouraudToLayer(y, x0, x1, i0, i1, colour0, colour1, quantiser);
        }
    }

    // As DrawSpanFilledDepthToLayer, blending across the span. The quantiser is asked at every
    // pixel since an interpolated colour differs at each one.
    private void DrawSpanFilledDepthGouraudToLayer(
        int y,
        float x0,
        float x1,
        float i0,
        float i1,
        in FastColor colour0,
        in FastColor colour1,
        IColourQuantiser? quantiser)
    {
        int start = Math.Max((int)MathF.Floor(x0), 0);
        int end = Math.Min((int)MathF.Floor(x1), (int)ScreenWidth - 1);
        float span = x1 - x0;

        for (int x = start; x <= end; x++)
        {
            float t = span <= 0 ? 0f : Math.Clamp((x - x0) / span, 0f, 1f);
            if (DepthTestLayer(x, y, i0 + ((i1 - i0) * t), surfaceId: 0))
            {
                FastColor colour = VertexColours.Lerp(colour0, colour1, t);
                _depthLayer!.SetPixel(x, y, quantiser == null ? colour : quantiser.Quantise(colour, x, y));
            }
        }
    }
}
