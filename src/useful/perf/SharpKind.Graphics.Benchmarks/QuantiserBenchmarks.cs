// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using BenchmarkDotNet.Attributes;
using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics.Benchmarks;

// The quantiser alone, walked in fill order so the dither's threshold lookup behaves as in a real frame; the docking profile puts this at 13-24 ns/pixel.
[JsonExporterAttribute.FullCompressed]
public class QuantiserBenchmarks
{
    private const int Height = 512;
    private const int Width = 512;

    private static readonly FastColor s_colour = new(255, 137, 91, 203);

    private readonly IColourQuantiser _channelGrid = new ChannelGridQuantiser(4);
    private readonly IColourQuantiser _palette = new PaletteQuantiser(SixteenEntries());
    private readonly IColourQuantiser _ditheredGrid;
    private readonly OrderedDitherQuantiser _ditheredPalette;

    public QuantiserBenchmarks()
    {
        _ditheredGrid = new OrderedDitherQuantiser(_channelGrid);
        _ditheredPalette = new OrderedDitherQuantiser(_palette);
    }

    // The loop itself, so every other number can have it subtracted.
    [Benchmark(Baseline = true)]
    public int Walk()
    {
        int sink = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                sink += x ^ y;
            }
        }

        return sink;
    }

    [Benchmark]
    public int ChannelGrid() => Walk(_channelGrid);

    [Benchmark]
    public int Palette() => Walk(_palette);

    [Benchmark]
    public int DitheredGrid() => Walk(_ditheredGrid);

    [Benchmark]
    public int DitheredPalette() => Walk(_ditheredPalette);

    // What a flat face could pay instead: the sixteen answers a 4x4 dither
    // can give for one colour, resolved once, then indexed per pixel.
    [Benchmark]
    public int DitherTableLookup()
    {
        FastColor[] cells = new FastColor[16];
        for (int cell = 0; cell < cells.Length; cell++)
        {
            cells[cell] = _ditheredPalette.Quantise(s_colour, cell & 3, cell >> 2);
        }

        int sink = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                sink += cells[((y & 3) << 2) | (x & 3)].R;
            }
        }

        return sink;
    }

    private static FastColor[] SixteenEntries()
    {
        FastColor[] entries = new FastColor[16];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = new(255, (byte)(i * 17), (byte)(255 - (i * 17)), (byte)(i * 9));
        }

        return entries;
    }

    private static int Walk(IColourQuantiser quantiser)
    {
        int sink = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                sink += quantiser.Quantise(s_colour, x, y).R;
            }
        }

        return sink;
    }
}
