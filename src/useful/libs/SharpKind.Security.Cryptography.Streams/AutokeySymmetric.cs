// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Autokey cipher. The key is the keyword, which may be empty. The key runs on from the message itself, so the message is transformed at once.
/// </summary>
public sealed class AutokeySymmetric : ClassicalSymmetricAlgorithm
{
    private static readonly Encoding s_encoding = new UnicodeEncoding(false, false);

    private Autokey _algorithm = new(new VigenereSettings());

    /// <inheritdoc />
    public override byte[] Key
    {
        get => s_encoding.GetBytes(_algorithm.Settings.Keyword);

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            try
            {
                _algorithm = new Autokey(new VigenereSettings() { Keyword = s_encoding.GetString(value) });
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
    /// <remarks>The keyword is 1 to 25 random letters.</remarks>
    public override void GenerateKey()
    {
        _algorithm.GenerateSettings();
        KeyValue = Key;
    }
}
