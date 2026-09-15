// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.GoldenFrames;

/// <summary>
/// Compares a run's frame signatures against the committed ones, and says
/// what moved when they disagree.
/// </summary>
/// <remarks>
/// The message is the reason this is not a bare hash comparison. A changed
/// hex string tells a reviewer that something changed and nothing else;
/// neither repo has an image diff, so the thumbnail grids printed side by
/// side are all there is to say whether the planet moved, the HUD vanished
/// or the backdrop painted over the world.
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
            if (expected[i].Hash != actual[i].Hash)
            {
                return Report(expected[i], actual[i]);
            }
        }

        return null;
    }

    // Both grids side by side, with the rows that differ marked, so the
    // failure message alone says what moved.
    private static string Report(FrameSignature expected, FrameSignature actual)
    {
        StringBuilder message = new();
        _ = message.Append(actual.Describe()).Append(" differs from ").Append(expected.Describe()).Append('\n');
        _ = message.Append("     expected                         actual\n");

        for (int row = 0; row < expected.Thumbnail.Count; row++)
        {
            string before = expected.Thumbnail[row];
            string after = row < actual.Thumbnail.Count ? actual.Thumbnail[row] : string.Empty;
            _ = message.Append(before == after ? "     " : "  != ");
            _ = message.Append(before).Append("  ").Append(after).Append('\n');
        }

        return message.ToString();
    }
}
