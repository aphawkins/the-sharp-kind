// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The homophonic substitution cipher. The key is entries separated by '|'. Each entry is a
/// plaintext character, a ':', then its homophones separated by ','. For example <c>A:M,B|B:A| :X</c>.
/// Plaintext characters cannot be ':' or '|', and homophones cannot hold ',' or '|'.
/// </summary>
public sealed class HomophonicSymmetric : ClassicalSymmetricAlgorithm
{
    private const char EntrySeparator = '|';
    private const char PlainSeparator = ':';
    private const char SymbolSeparator = ',';

    private static readonly Encoding s_encoding = new UnicodeEncoding(false, false);

    private Homophonic _algorithm = new(new HomophonicSettings());

    /// <inheritdoc />
    public override byte[] Key
    {
        get
        {
            IEnumerable<string> entries = _algorithm.Settings.Homophones
                .OrderBy(static pair => pair.Key)
                .Select(static pair => $"{pair.Key}{PlainSeparator}{string.Join(SymbolSeparator, pair.Value)}");

            return s_encoding.GetBytes(string.Join(EntrySeparator, entries));
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
    private protected override ICipher Cipher => _algorithm;

    /// <inheritdoc />
    private protected override bool NeedsWholeMessage => true;

    /// <inheritdoc />
    /// <remarks>Shuffles the homophones between the plaintext characters.</remarks>
    public override void GenerateKey()
    {
        _algorithm.GenerateSettings();
        KeyValue = Key;
    }

    private static Dictionary<char, IReadOnlyList<string>> ParseKey(string key)
    {
        Dictionary<char, IReadOnlyList<string>> homophones = [];

        foreach (string entry in key.Split(EntrySeparator))
        {
            if (entry.Length < 3 || entry[1] != PlainSeparator || !homophones.TryAdd(entry[0], entry[2..].Split(SymbolSeparator)))
            {
                throw new ArgumentException("Invalid format.", nameof(key));
            }
        }

        return homophones;
    }
}
