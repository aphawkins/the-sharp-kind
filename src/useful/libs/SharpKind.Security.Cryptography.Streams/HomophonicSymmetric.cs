// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The homophonic substitution cipher. The key is entries separated by '|'. Each entry is a
/// plaintext character, a ':', then its homophones separated by ','. For example <c>A:M,B|B:A| :X</c>.
/// Plaintext characters cannot be ':' or '|', and homophones cannot hold ',' or '|'.
/// </summary>
public sealed class HomophonicSymmetric : SymmetricAlgorithm
{
    private const char EntrySeperator = '|';
    private const char PlainSeperator = ':';
    private const char SymbolSeperator = ',';

    private static readonly Encoding s_encoding = new UnicodeEncoding(false, false);

    private Homophonic _algorithm;

    /// <summary>
    /// Initializes a new instance of the <see cref="HomophonicSymmetric"/> class.
    /// </summary>
    public HomophonicSymmetric()
    {
        Reset();
        _algorithm = new Homophonic(new HomophonicSettings());
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
            IEnumerable<string> entries = _algorithm.Settings.Homophones
                .OrderBy(static pair => pair.Key)
                .Select(static pair => $"{pair.Key}{PlainSeperator}{string.Join(SymbolSeperator, pair.Value)}");

            return s_encoding.GetBytes(string.Join(EntrySeperator, entries));
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            try
            {
                _algorithm = new Homophonic(new HomophonicSettings(ParseKey(s_encoding.GetString(value))));
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

    private static Dictionary<char, IReadOnlyList<string>> ParseKey(string key)
    {
        Dictionary<char, IReadOnlyList<string>> homophones = [];

        foreach (string entry in key.Split(EntrySeperator))
        {
            if (entry.Length < 3 || entry[1] != PlainSeperator || !homophones.TryAdd(entry[0], entry[2..].Split(SymbolSeperator)))
            {
                throw new ArgumentException("Invalid format.", nameof(key));
            }
        }

        return homophones;
    }

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
