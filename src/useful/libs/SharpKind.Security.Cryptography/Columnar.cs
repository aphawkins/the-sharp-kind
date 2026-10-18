// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Columnar transposition cipher: letters are written in rows under the keyword, then read off
/// column by column in alphabetical order of the keyword letters.
/// </summary>
/// <remarks>
/// The grid is irregular: the last row is not padded. Repeated keyword letters are read left to right.
/// Only letters are transposed; every other character stays where it was.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class Columnar(IColumnarSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "Columnar";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IColumnarSettings Settings { get; private set; } = settings;

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
    public void GenerateSettings() => Settings = ColumnarSettingsGenerator.Generate();

    /// <inheritdoc />
    public override string ToString() => CipherName;

    /// <summary>
    /// Gets the letter positions in the order the columns are read off.
    /// </summary>
    private int[] ReadingOrder(int length)
    {
        string keyword = Settings.Keyword;
        int columns = keyword.Length;

        return
        [
            .. Enumerable.Range(0, columns)
                .OrderBy(column => keyword[column])
                .ThenBy(column => column)
                .SelectMany(column => Enumerable.Range(0, length).Where(i => i % columns == column)),
        ];
    }
}
