// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Caesar cipher.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Caesar"/> class.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class Caesar(ICaesarSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "Caesar";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public ICaesarSettings Settings { get; private set; } = settings;

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        return Alphabet.Map(
            ciphertext,
            Settings.RightShift,
            static (shift, index) => (index + Alphabet.Length - shift) % Alphabet.Length);
    }

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        return Alphabet.Map(
            plaintext,
            Settings.RightShift,
            static (shift, index) => (index + shift) % Alphabet.Length);
    }

    /// <summary>
    /// Generates random settings.
    /// </summary>
    public void GenerateSettings() => Settings = CaesarSettingsGenerator.Generate();

    /// <inheritdoc />
    public override string ToString() => CipherName;
}
