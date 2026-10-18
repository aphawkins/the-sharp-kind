// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// A reflector MonoAlphabetic cipher. A character encrypts and decrypts back to the same character.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Reflector"/> class.
/// </remarks>
/// <param name="settings">The cipher's settings.</param>
public sealed class Reflector(IReflectorSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "Reflector";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IReflectorSettings Settings { get; private set; } = settings;

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
    public void GenerateSettings() => Settings = ReflectorSettingsGenerator.Generate();

    /// <inheritdoc />
    public override string ToString() => CipherName;

    private char Decrypt(char ciphertext) => Settings.Reflect(char.ToUpperInvariant(ciphertext));

    private char Encrypt(char plaintext) => Settings.GetSubstitution(char.ToUpperInvariant(plaintext));
}
