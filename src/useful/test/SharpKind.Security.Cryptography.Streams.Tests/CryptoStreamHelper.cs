// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;

namespace SharpKind.Security.Cryptography.Streams.Tests;

/// <summary>
/// Pushes text through a <see cref="CryptoStream"/> in chunks, as a caller of the stream ciphers would.
/// </summary>
internal static class CryptoStreamHelper
{
    /// <summary>
    /// Writes the text to a <see cref="CryptoStream"/> in chunks of the given length, and reads the result.
    /// </summary>
    /// <param name="transform">The transform of the cipher.</param>
    /// <param name="text">The text to write.</param>
    /// <param name="chunkLength">How many characters to write at a time.</param>
    /// <param name="paddingBefore">How many unused characters to put in front of each chunk in the buffer that is written.</param>
    /// <returns>The transformed text.</returns>
    internal static string Transform(ICryptoTransform transform, string text, int chunkLength, int paddingBefore = 0)
    {
        using MemoryStream output = new();

        // Encode the whole text first, so that a chunk can end between the two halves of a surrogate pair.
        byte[] bytes = Encoding.Unicode.GetBytes(text);
        int padding = paddingBefore * 2;

        using (CryptoStream crypto = new(output, transform, CryptoStreamMode.Write, leaveOpen: true))
        {
            for (int i = 0; i < bytes.Length; i += chunkLength * 2)
            {
                int count = Math.Min(chunkLength * 2, bytes.Length - i);
                byte[] buffer = [.. Encoding.Unicode.GetBytes(new string('?', paddingBefore)), .. bytes.AsSpan(i, count)];
                crypto.Write(buffer, padding, count);
            }
        }

        return Encoding.Unicode.GetString(output.ToArray());
    }
}
