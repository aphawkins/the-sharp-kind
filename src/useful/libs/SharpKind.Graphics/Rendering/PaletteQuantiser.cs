// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Rendering;

/// <summary>
/// An indexed display: a colour becomes the palette entry closest to it,
/// because the machine could show nothing the palette does not name.
/// </summary>
/// <remarks>
/// Nearest by squared distance in RGB, which is crude as colour science and
/// exactly what an artist choosing the closest pen from a fixed set would have
/// done.
/// </remarks>
public sealed class PaletteQuantiser : IColourQuantiser
{
    private readonly FastColor[] _entries;

    public PaletteQuantiser(IEnumerable<FastColor> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _entries = [.. entries];

        if (_entries.Length == 0)
        {
            throw new ArgumentException("A palette with no entries can display nothing.", nameof(entries));
        }

        LevelGap = MeanNeighbourGap(_entries);
    }

    public int Period => 1;

    // A hand-picked palette has no even spacing to read off, so this averages distance to the nearest neighbour, converted to a gap per channel.
    public float LevelGap { get; }

    public FastColor Quantise(in FastColor colour, int x, int y)
    {
        FastColor best = _entries[0];
        int bestDistance = int.MaxValue;

        foreach (FastColor candidate in _entries)
        {
            int dr = candidate.R - colour.R;
            int dg = candidate.G - colour.G;
            int db = candidate.B - colour.B;
            int distance = (dr * dr) + (dg * dg) + (db * db);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return new(colour.A, best.R, best.G, best.B);
    }

    private static float MeanNeighbourGap(FastColor[] entries)
    {
        if (entries.Length < 2)
        {
            return 1f;
        }

        double total = 0;

        foreach (FastColor entry in entries)
        {
            int nearest = int.MaxValue;

            foreach (FastColor other in entries)
            {
                if (other == entry)
                {
                    continue;
                }

                int dr = other.R - entry.R;
                int dg = other.G - entry.G;
                int db = other.B - entry.B;
                nearest = Math.Min(nearest, (dr * dr) + (dg * dg) + (db * db));
            }

            total += Math.Sqrt(nearest);
        }

        // The distance is across three channels at once; the caller wants the
        // gap along one.
        return (float)(total / entries.Length / Math.Sqrt(3));
    }
}
