// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The book cipher. The key is the key text, which must hold at least one letter.
/// </summary>
public sealed class BookCipherSymmetric : ClassicalSymmetricAlgorithm
{
    private static readonly Encoding s_encoding = new UnicodeEncoding(false, false);

    private BookCipher _algorithm = new(new BookCipherSettings("A"));

    /// <inheritdoc />
    public override byte[] Key
    {
        get => s_encoding.GetBytes(_algorithm.Settings.KeyText);

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            try
            {
                _algorithm = new BookCipher(new BookCipherSettings(s_encoding.GetString(value)));
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException("Argument exception.", nameof(value), ex);
            }

            base.Key = value;
        }
    }

    /// <inheritdoc />
    private protected override ICipher Cipher => _algorithm;

    /// <inheritdoc />
    private protected override bool NeedsWholeMessage => true;

    /// <inheritdoc />
    /// <remarks>
    /// The key is a text that both sides already have, such as a book, so a random one is of no use.
    /// This keeps the current key text.
    /// </remarks>
    public override void GenerateKey() => KeyValue = Key;
}
