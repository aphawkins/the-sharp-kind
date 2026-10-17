// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography.Tests;

/// <summary>
/// Decodes uuencoded text. Stages 9 and 10 of Singh's cipher challenge are published this way.
/// </summary>
internal static class UuDecoder
{
    private const int GroupBytes = 3;
    private const int GroupChars = 4;

    /// <summary>
    /// Decodes uuencoded text. The 'begin' and 'end' lines are optional, and lines that lost trailing spaces are accepted.
    /// </summary>
    /// <param name="text">The uuencoded text.</param>
    /// <returns>The decoded bytes.</returns>
    internal static byte[] Decode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        List<byte> result = [];

        foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = line.TrimEnd('\r');

            if (trimmed.StartsWith("begin ", StringComparison.Ordinal))
            {
                continue;
            }

            int length = Value(trimmed[0]);

            if (length == 0)
            {
                // A zero length line ends the data: the 'end' line follows it.
                break;
            }

            int chars = (length + GroupBytes - 1) / GroupBytes * GroupChars;
            string data = trimmed[1..].PadRight(chars);

            for (int i = 0; i < chars; i += GroupChars)
            {
                int a = Value(data[i]);
                int b = Value(data[i + 1]);
                int c = Value(data[i + 2]);
                int d = Value(data[i + 3]);

                result.Add((byte)((a << 2) | (b >> 4)));
                result.Add((byte)((b << 4) | (c >> 2)));
                result.Add((byte)((c << 6) | d));
            }

            result.RemoveRange(result.Count - ((chars / GroupChars * GroupBytes) - length), (chars / GroupChars * GroupBytes) - length);
        }

        return [.. result];
    }

    private static int Value(char c) => (c - ' ') & 0x3F;
}
