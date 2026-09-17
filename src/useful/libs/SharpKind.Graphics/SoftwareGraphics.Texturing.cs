// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpKind.Graphics;

// Bilinear filtering plus a box-filtered mip chain, so distant/foreshortened faces (SCR's road,
// nearly edge-on) sample a pre-averaged level instead of aliasing.
public sealed partial class SoftwareGraphics
{
    // Mip chains are cached per source texture (they never change once
    // built) rather than threaded through every caller, so the affine and
    // depth-tested texture paths stay call-compatible with the rest of the
    // renderer.
    private static readonly ConditionalWeakTable<FastBitmap, FastBitmap[]> s_mipChainCache = [];

    // Sample the texture at a [0,1] coordinate with bilinear filtering,
    // clamping at the edges.
    private static FastColor SampleTexture(FastBitmap texture, Vector2 uv)
    {
        float fx = (uv.X * texture.Width) - 0.5f;
        float fy = (uv.Y * texture.Height) - 0.5f;
        int x0 = (int)MathF.Floor(fx);
        int y0 = (int)MathF.Floor(fy);
        float tx = fx - x0;
        float ty = fy - y0;

        int x0c = Math.Clamp(x0, 0, texture.Width - 1);
        int x1c = Math.Clamp(x0 + 1, 0, texture.Width - 1);
        int y0c = Math.Clamp(y0, 0, texture.Height - 1);
        int y1c = Math.Clamp(y0 + 1, 0, texture.Height - 1);

        FastColor c00 = texture.GetPixel(x0c, y0c);
        FastColor c10 = texture.GetPixel(x1c, y0c);
        FastColor c01 = texture.GetPixel(x0c, y1c);
        FastColor c11 = texture.GetPixel(x1c, y1c);

        return new FastColor(
            LerpChannel(c00.A, c10.A, c01.A, c11.A, tx, ty),
            LerpChannel(c00.R, c10.R, c01.R, c11.R, tx, ty),
            LerpChannel(c00.G, c10.G, c01.G, c11.G, tx, ty),
            LerpChannel(c00.B, c10.B, c01.B, c11.B, tx, ty));
    }

    private static byte LerpChannel(byte c00, byte c10, byte c01, byte c11, float tx, float ty)
    {
        float top = c00 + ((c10 - c00) * tx);
        float bottom = c01 + ((c11 - c01) * tx);
        return (byte)Math.Clamp(MathF.Round(top + ((bottom - top) * ty)), 0f, 255f);
    }

    private static FastBitmap[] GetMipChain(FastBitmap texture) => s_mipChainCache.GetValue(texture, BuildMipChain);

    // Box-filtered mip chain, halving each dimension (floor at 1) until both
    // reach 1.
    private static FastBitmap[] BuildMipChain(FastBitmap texture)
    {
        List<FastBitmap> chain = [texture];
        FastBitmap current = texture;
        while (current.Width > 1 || current.Height > 1)
        {
            current = DownsampleHalf(current);
            chain.Add(current);
        }

        return [.. chain];
    }

    private static FastBitmap DownsampleHalf(FastBitmap src)
    {
        int width = Math.Max(1, src.Width / 2);
        int height = Math.Max(1, src.Height / 2);
        FastBitmap dst = new(width, height);

        for (int y = 0; y < height; y++)
        {
            int sy0 = Math.Min(y * 2, src.Height - 1);
            int sy1 = Math.Min((y * 2) + 1, src.Height - 1);
            for (int x = 0; x < width; x++)
            {
                dst.SetPixel(x, y, BoxAverage(src, x, sy0, sy1));
            }
        }

        return dst;
    }

    private static FastColor BoxAverage(FastBitmap src, int x, int sy0, int sy1)
    {
        int sx0 = Math.Min(x * 2, src.Width - 1);
        int sx1 = Math.Min((x * 2) + 1, src.Width - 1);

        FastColor c00 = src.GetPixel(sx0, sy0);
        FastColor c10 = src.GetPixel(sx1, sy0);
        FastColor c01 = src.GetPixel(sx0, sy1);
        FastColor c11 = src.GetPixel(sx1, sy1);

        return new FastColor(
            (byte)((c00.A + c10.A + c01.A + c11.A) / 4),
            (byte)((c00.R + c10.R + c01.R + c11.R) / 4),
            (byte)((c00.G + c10.G + c01.G + c11.G) / 4),
            (byte)((c00.B + c10.B + c01.B + c11.B) / 4));
    }

    // From the ratio of texel-space to screen-space area, computed once for the whole triangle since these triangles are small.
    private static FastBitmap SelectMip(
        FastBitmap[] mipChain,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 ta,
        Vector2 tb,
        Vector2 tc)
    {
        float screenArea = MathF.Abs(TriangleArea2(a, b, c));
        if (screenArea < 1e-4f)
        {
            return mipChain[0];
        }

        Vector2 texSize = new(mipChain[0].Width, mipChain[0].Height);
        float texelArea = MathF.Abs(TriangleArea2(ta * texSize, tb * texSize, tc * texSize));
        return mipChain[LevelForTexelsPerPixel(texelArea / screenArea, mipChain.Length, areaRatio: true)];
    }

    // From texels-per-pixel at each end of the visible run; see DrawSpanTexturedDepth.
    private static FastBitmap SelectMip(
        FastBitmap[] mipChain,
        float x0,
        float x1,
        int start,
        int end,
        float inverseDepth0,
        float inverseDepth1,
        Vector2 uv0,
        Vector2 uv1)
    {
        float span = x1 - x0;
        float lerpAtStart = span <= 0 ? 0f : Math.Clamp((start - x0) / span, 0f, 1f);
        float lerpAtEnd = span <= 0 ? 0f : Math.Clamp((end - x0) / span, 0f, 1f);
        float inverseDepthAtStart = inverseDepth0 + ((inverseDepth1 - inverseDepth0) * lerpAtStart);
        float inverseDepthAtEnd = inverseDepth0 + ((inverseDepth1 - inverseDepth0) * lerpAtEnd);
        if (inverseDepthAtStart <= 0 || inverseDepthAtEnd <= 0)
        {
            return mipChain[0];
        }

        Vector2 spanUvStart = Vector2.Lerp(uv0, uv1, lerpAtStart) / inverseDepthAtStart;
        Vector2 spanUvEnd = Vector2.Lerp(uv0, uv1, lerpAtEnd) / inverseDepthAtEnd;

        Vector2 texSize = new(mipChain[0].Width, mipChain[0].Height);
        float texelSpan = Vector2.Distance(spanUvStart * texSize, spanUvEnd * texSize);
        float pixelSpan = MathF.Max(end - start, 1);
        return mipChain[LevelForTexelsPerPixel(texelSpan / pixelSpan, mipChain.Length, areaRatio: false)];
    }

    // A ratio of areas needs halving twice (each mip level halves both
    // dimensions) to fall by one level; a ratio of lengths needs halving
    // once.
    private static int LevelForTexelsPerPixel(float texelsPerPixel, int chainLength, bool areaRatio)
    {
        if (texelsPerPixel <= 1f || float.IsNaN(texelsPerPixel) || float.IsInfinity(texelsPerPixel))
        {
            return 0;
        }

        float level = MathF.Log2(texelsPerPixel) * (areaRatio ? 0.5f : 1f);
        return Math.Clamp((int)MathF.Floor(level), 0, chainLength - 1);
    }

    private static float TriangleArea2(Vector2 a, Vector2 b, Vector2 c)
        => ((b.X - a.X) * (c.Y - a.Y)) - ((c.X - a.X) * (b.Y - a.Y));
}
