// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Settings for the homophonic substitution cipher. The default maps each letter to itself.
/// </summary>
public sealed record HomophonicSettings : IHomophonicSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HomophonicSettings"/> class.
    /// </summary>
    public HomophonicSettings()
        : this(Enumerable.Range('A', 26).ToDictionary(
            static c => (char)c,
            static c => (IReadOnlyList<string>)[((char)c).ToString()]))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HomophonicSettings"/> class.
    /// </summary>
    /// <param name="homophones">The homophones for each plaintext character.</param>
    /// <exception cref="ArgumentException">
    /// There must be at least one plaintext character, and each needs at least one homophone.
    /// Plaintext characters must be upper case. Homophones must be unique, upper case, without
    /// white space, and all the same length.
    /// </exception>
    public HomophonicSettings(IReadOnlyDictionary<char, IReadOnlyList<string>> homophones)
    {
        ArgumentNullException.ThrowIfNull(homophones);

        if (homophones.Count == 0)
        {
            throw new ArgumentException("There must be at least one plaintext character.", nameof(homophones));
        }

        HashSet<string> used = [];
        int length = 0;

        foreach ((char plain, IReadOnlyList<string> symbols) in homophones)
        {
            if (plain != char.ToUpperInvariant(plain) || symbols.Count == 0)
            {
                throw new ArgumentException(
                    $"Character '{plain}' must be upper case and have homophones.",
                    nameof(homophones));
            }

            foreach (string symbol in symbols)
            {
                length = length == 0 ? symbol.Length : length;

                if (symbol.Length != length
                    || symbol.Any(static c => char.IsWhiteSpace(c) || c != char.ToUpperInvariant(c))
                    || !used.Add(symbol))
                {
                    throw new ArgumentException(
                        $"Homophone '{symbol}' must be unique, upper case, without white space, {length} long.",
                        nameof(homophones));
                }
            }
        }

        SymbolLength = length;
        Homophones = homophones;
    }

    /// <inheritdoc />
    public int SymbolLength { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<char, IReadOnlyList<string>> Homophones { get; }
}
