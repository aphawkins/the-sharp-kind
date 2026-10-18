// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Accesses the Vigenere algorithm.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Vigenere"/> class.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class Vigenere(IVigenereSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "Vigenere";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IVigenereSettings Settings { get; private set; } = settings;

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        return Crypt(plaintext, isEncrypting: true);
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        return Crypt(ciphertext, isEncrypting: false);
    }

    /// <summary>
    /// Generates random settings.
    /// </summary>
    public void GenerateSettings() => Settings = VigenereSettingsGenerator.Generate();

    /// <inheritdoc />
    public override string ToString() => CipherName;

    /// <summary>
    /// Shifts each letter of <paramref name="text"/> by the matching letter of a non-empty keyword.
    /// </summary>
    /// <param name="text">The text to encrypt or decrypt.</param>
    /// <param name="keyword">The keyword, which must be non-empty upper case letters.</param>
    /// <param name="isEncrypting">Shift right when encrypting, left when decrypting.</param>
    /// <returns>The transformed text.</returns>
    private static string Crypt(string text, string keyword, bool isEncrypting)
        => string.Create(
            text.Length,
            (Text: text, Keyword: keyword, IsEncrypting: isEncrypting),
            static (chars, args) =>
            {
                int keywordPosition = 0;

                for (int i = 0; i < args.Text.Length; i++)
                {
                    char letter = args.Text[i];
                    int index = Alphabet.IndexOf(letter);

                    if (index < 0)
                    {
                        chars[i] = letter;
                        continue;
                    }

                    int shift = args.Keyword[keywordPosition % args.Keyword.Length] - 'A';
                    keywordPosition++;

                    if (!args.IsEncrypting)
                    {
                        shift = Alphabet.Length - shift;
                    }

                    chars[i] = (char)('A' + ((index + shift) % Alphabet.Length));
                }
            });

    /// <summary>
    /// Shifts each letter by the keyword letter at the same position, wrapping the keyword as needed.
    /// Only letters consume a keyword position; everything else passes through untouched.
    /// </summary>
    /// <param name="text">The text to encrypt or decrypt.</param>
    /// <param name="isEncrypting">Shift right when encrypting, left when decrypting.</param>
    /// <returns>The transformed text.</returns>
    private string Crypt(string text, bool isEncrypting)
    {
        string keyword = Settings.Keyword;

        // An empty keyword shifts by nothing, which a keyword of "A" also does.
        return Crypt(text, string.IsNullOrEmpty(keyword) ? "A" : keyword, isEncrypting);
    }
}
