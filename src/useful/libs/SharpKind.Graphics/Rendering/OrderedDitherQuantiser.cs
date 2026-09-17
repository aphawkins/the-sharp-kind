// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Rendering;

/// <summary>
/// Ordered (Bayer) dithering over whatever the rendition can actually display:
/// nudge the wanted colour up or down by an amount that depends on where the
/// pixel falls in a repeating threshold matrix, then take the nearest
/// displayable colour as usual.
/// </summary>
/// <remarks>
/// Neighbouring pixels get different nudges, so a colour that sits between two
/// displayable ones resolves to a mix of both in the proportion that averages
/// out to it. That buys back shades a limited palette cannot hold - the
/// station's six greys become an apparent forty-odd - at the cost of visible
/// texture on a flat face, which is exactly the trade the hardware these
/// renditions stand in for made.
/// <para>
/// It decorates a plain quantiser rather than replacing one, because what a
/// display can show is the rendition's business and dithering only changes how
/// a colour is chosen from that set.
/// </para>
/// </remarks>
public sealed class OrderedDitherQuantiser : IColourQuantiser
{
    // Small enough not to read as a texture of its own at these resolutions, large enough for sixteen levels between two colours.
    private static readonly int[] s_threshold =
    [
        0, 8, 2, 10,
        12, 4, 14, 6,
        3, 11, 1, 9,
        15, 7, 13, 5,
    ];

    private readonly IColourQuantiser _inner;

    // The nudge a cell applies never changes, so the whole matrix resolves to
    // sixteen amounts at construction rather than to arithmetic per pixel.
    private readonly float[] _nudges = new float[16];

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedDitherQuantiser"/> class,
    /// dithering over what the given quantiser can display.
    /// </summary>
    /// <param name="inner">What the rendition can display.</param>
    public OrderedDitherQuantiser(IColourQuantiser inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;

        // -0.5..+0.5 of a step, centred so dithering doesn't shift a face's average brightness; spans one gap so it never dithers
        // between non-neighbouring colours. The +0.5 centres each cell so the range is open, not [-0.5, +0.5), else the lowest
        // cell would land exactly on the midpoint between two displayable colours.
        for (int cell = 0; cell < _nudges.Length; cell++)
        {
            _nudges[cell] = (((s_threshold[cell] + 0.5f) / 16f) - 0.5f) * inner.LevelGap;
        }
    }

    // The Bayer matrix is 4x4, so the nudge repeats every four pixels.
    public int Period => 4;

    // Dithering picks differently among the same colours; it does not change
    // how far apart they are.
    public float LevelGap => _inner.LevelGap;

    public FastColor Quantise(in FastColor colour, int x, int y)
    {
        float nudge = _nudges[((y & 3) << 2) | (x & 3)];

        return _inner.Quantise(
            new(colour.A, Nudge(colour.R, nudge), Nudge(colour.G, nudge), Nudge(colour.B, nudge)),
            x,
            y);
    }

    private static byte Nudge(byte channel, float nudge)
        => (byte)Math.Clamp(channel + nudge, 0f, 255f);
}
