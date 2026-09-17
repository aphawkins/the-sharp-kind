// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SharpKind.Graphics;

namespace SharpKind.GoldenFrames;

// A form small enough to commit and legible enough to review. The thumbnail is what is compared, cell by cell;
// the hash only names a frame.
public sealed record FrameSignature(int Tick, string Hash, IReadOnlyList<string> Thumbnail)
{
    // Enough to place the planet disc, viewport border and HUD band; small enough to keep frames at a few kilobytes.
    private const int Cells = 32;

    // '.' rather than ' ' for empty space so trailing cells survive a trim and rows stay aligned in a diff.
    private const string Ramp = ".:-=+*#%@";

    // One step of drift is codegen, not a rendering change: the JIT may contract a multiply-add into an FMA,
    // which varies by machine and is worth a pixel of brightness. A real change moves a cell further.
    private const int CellTolerance = 1;

    public static FrameSignature Capture(int tick, FastBitmap frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return new(tick, HashOf(frame), ThumbnailOf(frame));
    }

    /// <summary>
    /// Whether two rows of the grid agree to within <see cref="CellTolerance"/>.
    /// </summary>
    public static bool RowsMatch(string expected, string actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        if (expected.Length != actual.Length)
        {
            return false;
        }

        for (int i = 0; i < expected.Length; i++)
        {
            int before = Ramp.IndexOf(expected[i], StringComparison.Ordinal);
            int after = Ramp.IndexOf(actual[i], StringComparison.Ordinal);

            // A character off the ramp cannot be off by a little.
            if (before < 0 || after < 0 || Math.Abs(before - after) > CellTolerance)
            {
                return false;
            }
        }

        return true;
    }

    public string Describe()
        => string.Create(CultureInfo.InvariantCulture, $"frame {Tick} ({Hash[..12]})");

    private static string HashOf(FastBitmap frame)
    {
        // Fed row by row: FastBitmap exposes pixels only through GetPixel.
        byte[] row = new byte[frame.Width * 4];
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        for (int y = 0; y < frame.Height; y++)
        {
            for (int x = 0; x < frame.Width; x++)
            {
                FastColor pixel = frame.GetPixel(x, y);
                int i = x * 4;
                row[i] = pixel.R;
                row[i + 1] = pixel.G;
                row[i + 2] = pixel.B;
                row[i + 3] = 0;
            }

            hash.AppendData(row);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static List<string> ThumbnailOf(FastBitmap frame)
    {
        List<string> rows = new(Cells);
        StringBuilder line = new(Cells);

        for (int cellY = 0; cellY < Cells; cellY++)
        {
            _ = line.Clear();
            for (int cellX = 0; cellX < Cells; cellX++)
            {
                _ = line.Append(Ramp[BrightnessOf(frame, cellX, cellY)]);
            }

            rows.Add(line.ToString());
        }

        return rows;
    }

    // Cell bounds are computed from the frame size, so any resolution produces a 32x32 grid comparable against the same baseline.
    private static int BrightnessOf(FastBitmap frame, int cellX, int cellY)
    {
        int left = cellX * frame.Width / Cells;
        int right = ((cellX + 1) * frame.Width / Cells) - 1;
        int top = cellY * frame.Height / Cells;
        int bottom = ((cellY + 1) * frame.Height / Cells) - 1;

        long total = 0;
        int count = 0;
        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                FastColor pixel = frame.GetPixel(x, y);

                // Rec. 601 luma, integer weights; the ramp has nine steps so nothing finer would show.
                total += ((299 * pixel.R) + (587 * pixel.G) + (114 * pixel.B)) / 1000;
                count++;
            }
        }

        if (count == 0)
        {
            return 0;
        }

        // Square-rooted rather than linear: both games draw thin bright lines on black, which a linear ramp would round to "empty".
        float mean = total / (count * 255f);
        return (int)((MathF.Sqrt(mean) * (Ramp.Length - 1)) + 0.5f);
    }
}
