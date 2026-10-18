// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Methods to aid cryptography.
/// </summary>
public static class CipherMethods
{
    private const int BufferLength = 4096;

    /// <summary>
    /// The encoding the transforms expect.
    /// </summary>
    private static readonly Encoding s_encoding = new UnicodeEncoding(false, false);

    /// <summary>
    /// Encrypts or decrypts a stream.
    /// </summary>
    /// <remarks>
    /// The input is read as UTF-16, unless it starts with a byte order mark for another encoding. The output is
    /// UTF-16. Both streams are left open.
    /// </remarks>
    /// <param name="cipher">The cipher to use.</param>
    /// <param name="transformMode">Defines the way in which the cipher will work.</param>
    /// <param name="input">The input stream.</param>
    /// <param name="output">The output stream.</param>
    public static void SymmetricTransform(SymmetricAlgorithm cipher, CipherTransformMode transformMode, Stream input, Stream output)
    {
        ArgumentNullException.ThrowIfNull(cipher);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        using ICryptoTransform transformer = GetTransformer(cipher, transformMode);
        using StreamReader reader = new(input, s_encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using CryptoStream crypto = new(output, transformer, CryptoStreamMode.Write, leaveOpen: true);

        // The encoder keeps the first half of a surrogate pair that ends a read, so the pair is not split.
        Encoder encoder = s_encoding.GetEncoder();
        char[] chars = new char[BufferLength];
        byte[] bytes = new byte[s_encoding.GetMaxByteCount(BufferLength)];
        int read;

        while ((read = reader.Read(chars, 0, chars.Length)) > 0)
        {
            crypto.Write(bytes, 0, encoder.GetBytes(chars, 0, read, bytes, 0, flush: false));
        }

        crypto.Write(bytes, 0, encoder.GetBytes(chars, 0, 0, bytes, 0, flush: true));
    }

    /// <summary>
    /// Enciphers or deciphers a string.
    /// </summary>
    /// <param name="cipher">The cipher to use.</param>
    /// <param name="transformMode">Defines the way in which the cipher will work.</param>
    /// <param name="input">The input text.</param>
    /// <returns>The output text.</returns>
    /// <exception cref="ArgumentNullException"> Thrown if <paramref name="cipher" /> or <paramref name="input"/> is null.</exception>
    public static string SymmetricTransform(SymmetricAlgorithm cipher, CipherTransformMode transformMode, string input)
    {
        ArgumentNullException.ThrowIfNull(cipher);
        ArgumentNullException.ThrowIfNull(input);

        using ICryptoTransform transformer = GetTransformer(cipher, transformMode);
        using MemoryStream outputStream = new();

        using (CryptoStream crypto = new(outputStream, transformer, CryptoStreamMode.Write, leaveOpen: true))
        {
            crypto.Write(s_encoding.GetBytes(input));
        }

        return s_encoding.GetString(outputStream.ToArray());
    }

    /// <summary>
    /// Gets a SymmetricAlgorithm's transformer.
    /// </summary>
    /// <param name="cipher">The SymmetricAlgorithm.</param>
    /// <param name="transformMode">The get the encrypt/decrypt transformer.</param>
    /// <returns>The SymmetricAlgorithm's transformer.</returns>
    private static ICryptoTransform GetTransformer(SymmetricAlgorithm cipher, CipherTransformMode transformMode)
    {
        if (transformMode == CipherTransformMode.Encrypt)
        {
            // Get an encryptor.
            return cipher.CreateEncryptor();
        }

        // Get an decryptor.
        return cipher.CreateDecryptor();
    }
}
