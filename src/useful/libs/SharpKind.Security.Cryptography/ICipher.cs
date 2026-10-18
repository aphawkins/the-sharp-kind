// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Interface that all ciphers should implement.
/// </summary>
/// <remarks>
/// These are classical ciphers, which have no notion of case: enciphered letters are always
/// returned in upper case, whatever the case of the input. Round tripping text through
/// <see cref="Encrypt(string)"/> and <see cref="Decrypt(string)"/> therefore returns the
/// original letters, but upper cased. Characters that a cipher does not encipher pass through
/// unchanged, unless the cipher's own remarks say otherwise.
/// </remarks>
public interface ICipher
{
    /// <summary>
    /// Gets the name of this cipher.
    /// </summary>
    public string CipherName { get; }

    /// <summary>
    /// Encrypts a plaintext string.
    /// </summary>
    /// <param name="plaintext">The text to encrypt.</param>
    /// <returns>The encrypted text.</returns>
    public string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts a ciphertext string.
    /// </summary>
    /// <param name="ciphertext">The text to decrypt.</param>
    /// <returns>The decrypted text.</returns>
    public string Decrypt(string ciphertext);

    /// <summary>
    /// Generates random settings.
    /// </summary>
    public void GenerateSettings();
}
