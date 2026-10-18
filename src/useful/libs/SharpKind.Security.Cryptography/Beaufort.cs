// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Beaufort cipher, which enciphers each letter as the keyword letter minus the plaintext letter.
/// </summary>
/// <remarks>
/// It takes the same keyword settings as <see cref="Vigenere"/>. Subtracting from the key is its own
/// inverse, so encrypting and decrypting are the same operation.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class Beaufort(IVigenereSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "Beaufort";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IVigenereSettings Settings { get; private set; } = settings;

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        string keyword = Settings.Keyword;

        // An empty keyword behaves like "A", which makes Beaufort an Atbash shifted by one.
        if (string.IsNullOrEmpty(keyword))
        {
            keyword = "A";
        }

        return string.Create(
            plaintext.Length,
            (Text: plaintext, Keyword: keyword),
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

                    int key = args.Keyword[keywordPosition % args.Keyword.Length] - 'A';
                    keywordPosition++;

                    chars[i] = (char)('A' + ((key - index + Alphabet.Length) % Alphabet.Length));
                }
            });
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext) => Encrypt(ciphertext);

    /// <summary>
    /// Generates random settings.
    /// </summary>
    public void GenerateSettings() => Settings = VigenereSettingsGenerator.Generate();

    /// <inheritdoc />
    public override string ToString() => CipherName;
}
