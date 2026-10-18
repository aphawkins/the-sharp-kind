// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The MonoAlphabetic cipher.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MonoAlphabetic"/> class.
/// </remarks>
/// <param name="settings">The cipher's settings.</param>
public sealed class MonoAlphabetic(IMonoAlphabeticSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "MonoAlphabetic";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IMonoAlphabeticSettings Settings { get; private set; } = settings;

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        StringBuilder sb = new(ciphertext.Length);

        for (int i = 0; i < ciphertext.Length; i++)
        {
            sb.Append(Decrypt(ciphertext[i]));
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        StringBuilder sb = new(plaintext.Length);

        for (int i = 0; i < plaintext.Length; i++)
        {
            sb.Append(Encrypt(plaintext[i]));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate random settings.
    /// </summary>
    public void GenerateSettings() => Settings = MonoAlphabeticSettingsGenerator.Generate();

    /// <inheritdoc />
    public override string ToString() => CipherName;

    private char Decrypt(char ciphertext) => Settings.Reverse(char.ToUpperInvariant(ciphertext));

    private char Encrypt(char plaintext) => Settings.GetSubstitution(char.ToUpperInvariant(plaintext));
}
