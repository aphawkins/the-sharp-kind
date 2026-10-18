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

        using (CryptoStream crypto = new(output, transform, CryptoStreamMode.Write, leaveOpen: true))
        {
            for (int i = 0; i < text.Length; i += chunkLength)
            {
                string chunk = string.Concat(new string('?', paddingBefore), text.AsSpan(i, Math.Min(chunkLength, text.Length - i)));
                byte[] bytes = Encoding.Unicode.GetBytes(chunk);
                crypto.Write(bytes, paddingBefore * 2, bytes.Length - (paddingBefore * 2));
            }
        }

        return Encoding.Unicode.GetString(output.ToArray());
    }
}
