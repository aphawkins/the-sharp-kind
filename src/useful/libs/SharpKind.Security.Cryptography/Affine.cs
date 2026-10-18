// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Affine cipher, which enciphers each letter x as (ax + b) mod 26.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Affine"/> class.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class Affine(IAffineSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "Affine";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IAffineSettings Settings { get; private set; } = settings;

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        // Undo the mapping by looking each letter up in a table of where encrypting sends it.
        int[] inverse = new int[Alphabet.Length];

        for (int index = 0; index < Alphabet.Length; index++)
        {
            inverse[Encipher(index)] = index;
        }

        return Alphabet.Map(ciphertext, inverse, static (table, index) => table[index]);
    }

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        return Alphabet.Map(plaintext, this, static (cipher, index) => cipher.Encipher(index));
    }

    /// <summary>
    /// Generates random settings.
    /// </summary>
    public void GenerateSettings() => Settings = AffineSettingsGenerator.Generate();

    /// <inheritdoc />
    public override string ToString() => CipherName;

    /// <summary>
    /// Enciphers a zero based alphabet position.
    /// </summary>
    private int Encipher(int index) => ((Settings.Multiplier * index) + Settings.Shift) % Alphabet.Length;
}
