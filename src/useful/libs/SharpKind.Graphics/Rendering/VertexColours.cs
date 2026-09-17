// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Rendering;

/// <summary>
/// The arithmetic a Gouraud fill does on the colours at a polygon's corners:
/// blending between them, and - where a fill cannot blend at all - standing
/// them down to the one colour that best replaces them.
/// </summary>
public static class VertexColours
{
    /// <summary>
    /// The colour a fraction <paramref name="t"/> of the way from
    /// <paramref name="from"/> to <paramref name="to"/>.
    /// </summary>
    /// <remarks>
    /// Alpha comes from <paramref name="from"/> rather than being blended: the
    /// corners of one face carry one face's alpha, and blending it would only
    /// invent values between two identical ones.
    /// </remarks>
    public static FastColor Lerp(in FastColor from, in FastColor to, float t) => new(
        from.A,
        Channel(from.R, to.R, t),
        Channel(from.G, to.G, t),
        Channel(from.B, to.B, t));

    /// <summary>
    /// The average of a face's corner colours - the single colour nearest to
    /// what interpolating between them all would produce.
    /// </summary>
    public static FastColor Mean(IReadOnlyList<FastColor> colours)
    {
        ArgumentNullException.ThrowIfNull(colours);

        if (colours.Count == 0)
        {
            return default;
        }

        int r = 0;
        int g = 0;
        int b = 0;

        foreach (FastColor colour in colours)
        {
            r += colour.R;
            g += colour.G;
            b += colour.B;
        }

        return new(
            colours[0].A,
            Average(r, colours.Count),
            Average(g, colours.Count),
            Average(b, colours.Count));
    }

    /// <summary>
    /// One flat colour for a face whose corners were shaded separately, for a
    /// fill that has no way to blend across it - the painter's chain, the
    /// wireframe outline, a backend with no per-pixel hook.
    /// </summary>
    /// <remarks>
    /// The result is quantised here, as a flat face's colour is quantised once
    /// before it is submitted; a dither is left alone, since only the fill can
    /// ask it, per pixel.
    /// </remarks>
    public static FastColor Flatten(IReadOnlyList<FastColor> colours, IColourQuantiser? quantiser)
    {
        FastColor mean = Mean(colours);

        return quantiser is { IsPositionDependent: false } ? quantiser.Quantise(mean, 0, 0) : mean;
    }

    // Away from zero, matching LambertShading and NearestLevel, so a half-way
    // channel does not round one way here and the other when a rendition
    // quantises it.
    private static byte Channel(byte from, byte to, float t)
        => (byte)MathF.Round(from + ((to - from) * t), MidpointRounding.AwayFromZero);

    // Same rounding, so the mean of black and white is the midpoint Lerp gives at 0.5, not one less through truncation.
    private static byte Average(int total, int count) => (byte)((total + (count / 2)) / count);
}
