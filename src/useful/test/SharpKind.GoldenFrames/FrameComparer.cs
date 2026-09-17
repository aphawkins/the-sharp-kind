// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.GoldenFrames;

/// <summary>
/// Compares a run's frame signatures against the committed ones, and says
/// what moved when they disagree.
/// </summary>
/// <remarks>
/// The thumbnail grid is compared, not the hash: a pixel-exact hash of a
/// floating-point render is pinned to the machine that regenerated it, and
/// fails elsewhere over rounding. The grids are also the message - neither
/// repo has an image diff, so printing them side by side is all there is to
/// say whether the planet moved, the HUD vanished or the backdrop painted
/// over the world.
/// </remarks>
public static class FrameComparer
{
    /// <summary>
    /// The first frame that differs, described for a human, or null when
    /// every frame matches.
    /// </summary>
    public static string? FindFirstDifference(
        IReadOnlyList<FrameSignature> expected,
        IReadOnlyList<FrameSignature> actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        if (expected.Count != actual.Count)
        {
            return $"frame count: expected {expected.Count}, got {actual.Count}";
        }

        for (int i = 0; i < expected.Count; i++)
        {
            if (Differs(expected[i], actual[i]))
            {
                return Report(expected[i], actual[i]);
            }
        }

        return null;
    }

    private static bool Differs(FrameSignature expected, FrameSignature actual)
    {
        if (expected.Thumbnail.Count != actual.Thumbnail.Count)
        {
            return true;
        }

        for (int row = 0; row < expected.Thumbnail.Count; row++)
        {
            if (!FrameSignature.RowsMatch(expected.Thumbnail[row], actual.Thumbnail[row]))
            {
                return true;
            }
        }

        return false;
    }

    // Both grids side by side, so the failure message alone says what moved.
    // Only the rows that broke the tolerance are marked: a row that drifted a
    // single step is not what failed.
    private static string Report(FrameSignature expected, FrameSignature actual)
    {
        StringBuilder message = new();
        _ = message.Append(actual.Describe()).Append(" differs from ").Append(expected.Describe()).Append('\n');
        _ = message.Append("     expected                         actual\n");

        for (int row = 0; row < expected.Thumbnail.Count; row++)
        {
            string before = expected.Thumbnail[row];
            string after = row < actual.Thumbnail.Count ? actual.Thumbnail[row] : string.Empty;
            _ = message.Append(FrameSignature.RowsMatch(before, after) ? "     " : "  != ");
            _ = message.Append(before).Append("  ").Append(after).Append('\n');
        }

        return message.ToString();
    }
}
