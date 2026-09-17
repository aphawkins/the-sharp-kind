// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics;

// The Gouraud fill: a colour per corner blended across the polygon. Its own file because the flat
// and textured rasterisers already fill SoftwareGraphics to the repo's per-file size limit.
public sealed partial class SoftwareGraphics
{
    public void DrawPolygonFilledDepth(
        Vector2[] points,
        float[] depths,
        FastColor[] vertexColors,
        IColourQuantiser? quantiser)
    {
        if (points == null
            || depths == null
            || vertexColors == null
            || depths.Length < points.Length
            || vertexColors.Length < points.Length)
        {
            return;
        }

        // Create triangles of which each share the first vertex
        for (int i = 1; i < points.Length - 1; i++)
        {
            DrawTriangleFilledDepthGouraud(
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
    }

    // Gouraud variant of DrawTriangleFilledDepth: blended down each edge and across each span.
    // Affine, not divided by depth - a shade has no perspective correctness to get right the way a
    // texture coordinate does, and the eye can't tell across one ship face.
    internal void DrawTriangleFilledDepthGouraud(
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

        // Sort the points so that a.Y <= b.Y <= c.Y, keeping each depth and
        // colour paired with its point
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

        // Clamp Y range to screen bounds
        int firstY = Math.Max((int)MathF.Ceiling(a.Y), _clipTop);
        int lastY = Math.Min((int)MathF.Floor(c.Y), _clipBottom - 1);

        // As DrawTriangleFilledDepth: evaluate the two edges crossing each
        // scanline directly, carrying the inverse depth and the colour along
        for (int y = firstY; y <= lastY; y++)
        {
            // the long edge a-c, and either a-b (above b) or b-c (below)
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

            DrawSpanFilledDepthGouraud(y, x0, x1, i0, i1, colour0, colour1, quantiser);
        }
    }

    // Same rounding as VertexColours.Lerp: t is clamped, so the result never leaves the range the two ends span.
    private static byte Channel(byte from, float range, float t) => (byte)(from + (range * t) + 0.5f);

    // Unlike the flat span, the quantiser is asked at every pixel: a blended colour differs per
    // pixel, so there's no one answer the caller could resolve for the whole face beforehand.
    private void DrawSpanFilledDepthGouraud(
        int y,
        float x0,
        float x1,
        float i0,
        float i1,
        in FastColor colour0,
        in FastColor colour1,
        IColourQuantiser? quantiser)
    {
        int start = Math.Max((int)MathF.Floor(x0), _clipLeft);
        int end = Math.Min((int)MathF.Floor(x1), _clipRight - 1);

        // Worked out once for the whole span, not per pixel: reciprocal width, and per-channel/depth deltas.
        float span = x1 - x0;
        float across = span <= 0 ? 0f : 1f / span;
        float depthRange = i1 - i0;
        float redRange = colour1.R - colour0.R;
        float greenRange = colour1.G - colour0.G;
        float blueRange = colour1.B - colour0.B;

        for (int x = start; x <= end; x++)
        {
            float t = Math.Clamp((x - x0) * across, 0f, 1f);
            if (DepthTest(x, y, i0 + (depthRange * t), surfaceId: 0))
            {
                FastColor colour = new(
                    colour0.A,
                    Channel(colour0.R, redRange, t),
                    Channel(colour0.G, greenRange, t),
                    Channel(colour0.B, blueRange, t));

                StorePixel(x, y, quantiser == null ? colour : quantiser.Quantise(colour, x, y));
            }
        }
    }
}
