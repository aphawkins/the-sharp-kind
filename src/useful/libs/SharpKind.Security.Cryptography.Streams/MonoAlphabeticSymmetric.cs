// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The MonoAlphabetic cipher.
/// </summary>
public sealed class MonoAlphabeticSymmetric : ClassicalSymmetricAlgorithm
{
    /// <summary>
    /// States how many parts there are in the key.
    /// </summary>
    private const int KeyParts = 2;

    /// <summary>
    /// The char that separates part of the key.
    /// </summary>
    private const char KeySeparator = '|';

    /// <summary>
    /// The encoding used by this cipher.
    /// </summary>
    private static readonly Encoding s_encoding = new UnicodeEncoding(false, false);

    private MonoAlphabetic _algorithm = new(new MonoAlphabeticSettings());

    /// <inheritdoc />
    public override byte[] Key
    {
        get
        {
            // CharacterSet|Substitutions
            StringBuilder key = new(_algorithm.Settings.CharacterSet);
            key.Append(KeySeparator)
                .Append(_algorithm.Settings.Substitutions);

            return s_encoding.GetBytes(key.ToString());
        }

        set
        {
            (string CharacterSet, string Substitutions) key;
            try
            {
                key = ParseKey(value);

                _algorithm = new MonoAlphabetic(new MonoAlphabeticSettings()
                {
                    CharacterSet = key.CharacterSet,
                    Substitutions = key.Substitutions,
                });
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
    public override void GenerateKey()
    {
        _algorithm.GenerateSettings();
        KeyValue = Key;
    }

    private static (string CharacterSet, string Substitutions) ParseKey(byte[] key)
    {
        // Example:
        // characterSet|substitutions
        ArgumentNullException.ThrowIfNull(key);

        if (key.SequenceEqual([]))
        {
            throw new ArgumentException("Invalid format.", nameof(key));
        }

        string keyString = s_encoding.GetString(key);

        string[] parts = keyString.Split([KeySeparator], StringSplitOptions.None);

        return parts.Length != KeyParts
            ? throw new ArgumentException("Incorrect number of key parts.", nameof(key))
            : ((string CharacterSet, string Substitutions))(parts[0], parts[1]);
    }
}
