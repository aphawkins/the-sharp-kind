// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// A reflector MonoAlphabetic cipher. A character encrypts and decrypts back to the same character.
/// </summary>
public sealed class ReflectorSymmetric : ClassicalSymmetricAlgorithm
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

    private Reflector _algorithm = new(new ReflectorSettings());

    /// <inheritdoc />
    public override byte[] Key
    {
        get
        {
            // CharacterSet|Substitutions
            StringBuilder key = new();
            key.Append([.. _algorithm.Settings.CharacterSet])
                .Append(KeySeparator)
                .Append([.. _algorithm.Settings.Substitutions]);
            return s_encoding.GetBytes(key.ToString());
        }

        set
        {
            try
            {
                (IList<char> characterSet, IList<char> substitutions) = ParseKey(value);
                _algorithm = new Reflector(new ReflectorSettings() { CharacterSet = characterSet, Substitutions = substitutions });
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

    private static (IList<char> CharacterSet, IList<char> Substitutions) ParseKey(byte[] key)
    {
        // Example:
        // CharacterSet|Substitutions
        ArgumentNullException.ThrowIfNull(key);

        if (key.SequenceEqual([]))
        {
            throw new ArgumentException("Invalid format.", nameof(key));
        }

        string keyString = s_encoding.GetString(key);

        string[] parts = keyString.Split([KeySeparator], StringSplitOptions.None);

        return parts.Length != KeyParts
            ? throw new ArgumentException("Incorrect number of key parts.", nameof(key))
            : ((IList<char> CharacterSet, IList<char> Substitutions))(parts[0].ToCharArray(), parts[1].ToCharArray());
    }
}
