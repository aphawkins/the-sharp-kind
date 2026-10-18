// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The book cipher: each plaintext letter becomes the position of a matching letter in a key text.
/// </summary>
/// <remarks>
/// Only the letters A-Z of the key text are counted. The ciphertext is positions separated by
/// spaces. Encrypting uses the first matching position of each letter and drops non-letters.
/// Decrypting accepts any occurrence, with any white space between positions.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class BookCipher(IBookCipherSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "Book";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IBookCipherSettings Settings { get; } = settings;

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        string letters = KeyLetters();
        List<string> positions = [];

        foreach (char character in plaintext.Where(static c => Alphabet.IndexOf(c) >= 0))
        {
            int index = letters.IndexOf(char.ToUpperInvariant(character), StringComparison.Ordinal);

            positions.Add(index < 0
                ? throw new ArgumentException($"Letter '{character}' is not in the key text.", nameof(plaintext))
                : (index + 1).ToString(CultureInfo.InvariantCulture));
        }

        return string.Join(' ', positions);
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        string letters = KeyLetters();
        StringBuilder result = new();

        foreach (string token in ciphertext.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int position)
                || position < 1
                || position > letters.Length)
            {
                throw new ArgumentException($"'{token}' is not a position in the key text.", nameof(ciphertext));
            }

            _ = result.Append(letters[position - 1]);
        }

        return result.ToString();
    }

    /// <inheritdoc />
    public override string ToString() => CipherName;

    /// <inheritdoc />
    /// <remarks>
    /// The key is a text that both sides already have, such as a book, so a random one is of no use.
    /// This keeps the current key text.
    /// </remarks>
    public void GenerateSettings()
    {
    }

    private string KeyLetters()
        => new([.. Settings.KeyText.Where(static c => Alphabet.IndexOf(c) >= 0).Select(char.ToUpperInvariant)]);
}
