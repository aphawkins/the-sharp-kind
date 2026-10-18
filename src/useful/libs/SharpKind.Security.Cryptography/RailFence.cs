// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Rail Fence cipher: letters are written in a zigzag down and up the rails, then read off rail by rail.
/// </summary>
/// <remarks>
/// Only letters are transposed; every other character stays where it was.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class RailFence(IRailFenceSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "Rail Fence";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IRailFenceSettings Settings { get; private set; } = settings;

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        return Transposition.Apply(plaintext, ReadingOrder, isEncrypting: true);
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        return Transposition.Apply(ciphertext, ReadingOrder, isEncrypting: false);
    }

    /// <summary>
    /// Generates random settings.
    /// </summary>
    public void GenerateSettings() => Settings = RailFenceSettingsGenerator.Generate();

    /// <inheritdoc />
    public override string ToString() => CipherName;

    /// <summary>
    /// Gets the letter positions in the order the rails are read off.
    /// </summary>
    private int[] ReadingOrder(int length)
    {
        int cycle = (2 * Settings.Rails) - 2;

        return
        [
            .. Enumerable.Range(0, length)
                .OrderBy(i => Math.Min(i % cycle, cycle - (i % cycle)))
                .ThenBy(i => i),
        ];
    }
}
