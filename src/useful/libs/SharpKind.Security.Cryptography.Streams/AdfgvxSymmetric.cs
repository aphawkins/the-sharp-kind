// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The ADFGVX style cipher. The key is <c>labels|square|keyword</c>, so the square cannot hold a '|'.
/// </summary>
public sealed class AdfgvxSymmetric : ClassicalSymmetricAlgorithm
{
    private const int KeyParts = 3;
    private const char KeySeparator = '|';

    private static readonly Encoding s_encoding = new UnicodeEncoding(false, false);

    private Adfgvx _algorithm = new(new AdfgvxSettings());

    /// <inheritdoc />
    public override byte[] Key
    {
        get
        {
            IAdfgvxSettings settings = _algorithm.Settings;
            return s_encoding.GetBytes($"{settings.Labels}{KeySeparator}{settings.Square}{KeySeparator}{settings.Keyword}");
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            string[] parts = s_encoding.GetString(value).Split(KeySeparator);

            try
            {
                _algorithm = parts.Length != KeyParts
                    ? throw new ArgumentException("Incorrect number of key parts.", nameof(value))
                    : new Adfgvx(new AdfgvxSettings(parts[0], parts[1], parts[2]));
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
    /// <remarks>Shuffles the square and picks a random keyword of the same length. The labels stay the same.</remarks>
    public override void GenerateKey()
    {
        _algorithm.GenerateSettings();
        KeyValue = Key;
    }
}
