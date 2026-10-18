// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Playfair cipher. The key is the keyword, which may be empty.
/// </summary>
public sealed class PlayfairSymmetric : ClassicalSymmetricAlgorithm
{
    private static readonly Encoding s_encoding = new UnicodeEncoding(false, false);

    private Playfair _algorithm = new(new PlayfairSettings());

    /// <inheritdoc />
    public override byte[] Key
    {
        get => s_encoding.GetBytes(_algorithm.Settings.Keyword);

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            try
            {
                _algorithm = new Playfair(new PlayfairSettings() { Keyword = s_encoding.GetString(value) });
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
    /// <remarks>The keyword is the grid letters in a random order.</remarks>
    public override void GenerateKey()
    {
        _algorithm.GenerateSettings();
        KeyValue = Key;
    }
}
