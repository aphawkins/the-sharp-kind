// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Autokey cipher: a Vigenere whose key is the keyword followed by the plaintext itself.
/// </summary>
/// <remarks>
/// It takes the same keyword settings as <see cref="Vigenere"/>. Only letters extend the key, and
/// non-letters pass through untouched.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class Autokey(IVigenereSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "Autokey";

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
    /// Shifts each letter by the next key letter, feeding each plaintext letter onto the end of the key.
    /// </summary>
    /// <param name="text">The text to encrypt or decrypt.</param>
    /// <param name="isEncrypting">Shift right when encrypting, left when decrypting.</param>
    /// <returns>The transformed text.</returns>
    private string Crypt(string text, bool isEncrypting)
    {
        string keyword = Settings.Keyword;

        // An empty keyword primes the key with "A", which shifts the first letter by nothing.
        Queue<int> key = new(string.IsNullOrEmpty(keyword) ? [0] : keyword.Select(static c => c - 'A'));
        char[] chars = new char[text.Length];

        for (int i = 0; i < text.Length; i++)
        {
            char letter = text[i];
            int index = Alphabet.IndexOf(letter);

            if (index < 0)
            {
                chars[i] = letter;
                continue;
            }

            int shift = key.Dequeue();
            int result = isEncrypting
                ? (index + shift) % Alphabet.Length
                : (index + Alphabet.Length - shift) % Alphabet.Length;

            key.Enqueue(isEncrypting ? index : result);
            chars[i] = (char)('A' + result);
        }

        return new string(chars);
    }
}
