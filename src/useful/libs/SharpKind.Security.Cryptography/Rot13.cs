// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The ROT13 cipher.
/// </summary>
public sealed class Rot13 : ICipher
{
    private const int Rotation = 13;

    /// <inheritdoc />
    public string CipherName => "ROT13";

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
        => Encrypt(ciphertext); // Rotating by half the alphabet is its own inverse

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        return Alphabet.Map(plaintext, 0, static (_, index) => (index + Rotation) % Alphabet.Length);
    }

    /// <inheritdoc />
    public override string ToString() => CipherName;

    /// <inheritdoc />
    /// <remarks>ROT13 always rotates by 13 and has no key, so this does nothing.</remarks>
    public void GenerateSettings()
    {
    }
}
