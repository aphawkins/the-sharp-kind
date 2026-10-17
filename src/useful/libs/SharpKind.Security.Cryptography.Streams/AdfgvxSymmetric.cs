// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The ADFGVX style cipher. The key is <c>labels|square|keyword</c>, so the square cannot hold a '|'.
/// </summary>
public sealed class AdfgvxSymmetric : SymmetricAlgorithm
{
    private const int KeyParts = 3;
    private const char KeySeperator = '|';

    private static readonly Encoding s_encoding = new UnicodeEncoding(false, false);

    private Adfgvx _algorithm;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdfgvxSymmetric"/> class.
    /// </summary>
    public AdfgvxSymmetric()
    {
        Reset();
        _algorithm = new Adfgvx(new AdfgvxSettings());
    }

    /// <inheritdoc />
    public override byte[] IV
    {
        get => [];
        set => _ = value;
    }

    /// <inheritdoc />
    public override byte[] Key
    {
        get
        {
            IAdfgvxSettings settings = _algorithm.Settings;
            return s_encoding.GetBytes($"{settings.Labels}{KeySeperator}{settings.Square}{KeySeperator}{settings.Keyword}");
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            string[] parts = s_encoding.GetString(value).Split(KeySeperator);

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
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        Key = rgbKey;
        IV = rgbIV ?? [];
        return new BufferedClassicalSymmetricTransform(_algorithm, CipherTransformMode.Decrypt);
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        Key = rgbKey;
        IV = rgbIV ?? [];
        return new BufferedClassicalSymmetricTransform(_algorithm, CipherTransformMode.Encrypt);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
    }

    /// <inheritdoc />
    public override void GenerateKey() => KeyValue = Key;

    /// <inheritdoc />
    public override string ToString() => _algorithm.CipherName;

    private void Reset()
    {
#pragma warning disable CA5358 // Do Not Use Unsafe Cipher Modes - this cipher is inherently unsafe
        ModeValue = CipherMode.ECB;
        PaddingValue = PaddingMode.None;
        KeySizeValue = 16;
        BlockSizeValue = 16;
        FeedbackSizeValue = 16;
        LegalBlockSizesValue = new KeySizes[1];
        LegalBlockSizesValue[0] = new KeySizes(16, 16, 16);
        LegalKeySizesValue = new KeySizes[1];
        LegalKeySizesValue[0] = new KeySizes(0, int.MaxValue, 16);

        KeyValue = [];
        IVValue = [];
    }
}
