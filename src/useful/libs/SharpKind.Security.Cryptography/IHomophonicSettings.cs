// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Homophonic substitution cipher settings.
/// </summary>
public interface IHomophonicSettings
{
    /// <summary>
    /// Gets the number of characters in every homophone.
    /// </summary>
    public int SymbolLength { get; }

    /// <summary>
    /// Gets the homophones for each upper case plaintext character, such as a letter or a space.
    /// </summary>
    public IReadOnlyDictionary<char, IReadOnlyList<string>> Homophones { get; }
}
