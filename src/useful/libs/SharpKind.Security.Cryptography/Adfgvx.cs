// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The ADFGVX style cipher: a fractionating Polybius square followed by a columnar transposition.
/// </summary>
/// <remarks>
/// The transposition scrambles positions, so it does not honour the pass-through contract of
/// <see cref="ICipher"/>. The square is case sensitive. A character that is not in the square is looked up in
/// upper case, and is dropped if it is still not there. Decrypting returns the square's characters as they are.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class Adfgvx(IAdfgvxSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "ADFGVX";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IAdfgvxSettings Settings { get; private set; } = settings;

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        string labels = Settings.Labels;
        int size = labels.Length;
        StringBuilder fractionated = new(plaintext.Length * 2);

        foreach (char character in plaintext)
        {
            int index = Settings.Square.IndexOf(character, StringComparison.Ordinal);
            if (index < 0)
            {
                index = Settings.Square.IndexOf(char.ToUpperInvariant(character), StringComparison.Ordinal);
            }

            if (index >= 0)
            {
                fractionated.Append(labels[index / size]).Append(labels[index % size]);
            }
        }

        string text = fractionated.ToString();
        int columns = Settings.Keyword.Length;
        StringBuilder result = new(text.Length);

        foreach (int column in ColumnOrder())
        {
            for (int i = column; i < text.Length; i += columns)
            {
                result.Append(text[i]);
            }
        }

        return result.ToString();
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        string labels = Settings.Labels;
        string text = new([.. ciphertext.ToUpperInvariant().Where(labels.Contains)]);

        if (text.Length % 2 != 0)
        {
            throw new ArgumentException("Ciphertext must contain an even number of labels.", nameof(ciphertext));
        }

        int columns = Settings.Keyword.Length;
        char[] fractionated = new char[text.Length];
        int position = 0;

        foreach (int column in ColumnOrder())
        {
            for (int i = column; i < text.Length; i += columns)
            {
                fractionated[i] = text[position++];
            }
        }

        int size = labels.Length;
        StringBuilder result = new(text.Length / 2);

        for (int i = 0; i < fractionated.Length; i += 2)
        {
            result.Append(Settings.Square[(labels.IndexOf(fractionated[i], StringComparison.Ordinal) * size)
                + labels.IndexOf(fractionated[i + 1], StringComparison.Ordinal)]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Generates random settings: a shuffled square and a random keyword of the same length, with the same labels.
    /// </summary>
    public void GenerateSettings() => Settings = AdfgvxSettingsGenerator.Generate(Settings);

    /// <inheritdoc />
    public override string ToString() => CipherName;

    /// <summary>
    /// Gets the keyword's column numbers in alphabetical order of their letters, earlier columns first when letters repeat.
    /// </summary>
    private IEnumerable<int> ColumnOrder()
        => Enumerable.Range(0, Settings.Keyword.Length).OrderBy(i => Settings.Keyword[i]).ThenBy(i => i);
}
