// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The reflector algorithm settings. A reflector is a monoalphabetic substitution in which
/// every substitution is its own inverse, so no letter substitutes to itself.
/// </summary>
public interface IReflectorSettings
{
    /// <summary>
    /// Gets substitutions.
    /// </summary>
    public IList<char> Substitutions { get; }

    /// <summary>
    /// Gets the character set.
    /// </summary>
    /// <value>The character set.</value>
    public IList<char> CharacterSet { get; }

    /// <summary>
    /// Gets the number of substitutions made. One distinct pair swapped equals one substitution.
    /// </summary>
    /// <value>The number of distinct substitutions.</value>
    /// <returns>The number of distinct substitutions made.</returns>
    public int SubstitutionCount { get; }

    /// <summary>
    /// Gets the letter that a letter substitutes to.
    /// </summary>
    /// <param name="letter">The letter to substitute.</param>
    /// <returns>The substituted letter, or <paramref name="letter"/> if it is not in the character set.</returns>
    public char GetSubstitution(char letter);

    /// <summary>
    /// Sets the letter that a letter substitutes to.
    /// </summary>
    /// <param name="substitution">The letter to substitute.</param>
    /// <param name="newSubstitution">The substitution to set.</param>
    public void SetSubstitution(char substitution, char newSubstitution);

    /// <summary>
    /// Gets the reverse substitution for a letter.
    /// </summary>
    /// <param name="letter">The letter to match.</param>
    /// <returns>The letter that substitutes to this letter.</returns>
    public char Reflect(char letter);
}
