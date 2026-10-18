// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The monoalphabetic algorithm settings.
/// </summary>
/// <remarks>
/// Set <see cref="CharacterSet"/> before <see cref="Substitutions"/>, as the substitutions are checked against it.
/// </remarks>
public sealed record MonoAlphabeticSettings : IMonoAlphabeticSettings
{
    private string _substitutions = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private bool _substitutionsInitialised;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The substitutions are already set.</exception>
    public string CharacterSet
    {
        get => field ??= "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        init
        {
            if (_substitutionsInitialised)
            {
                throw new InvalidOperationException("Set the character set before the substitutions.");
            }

            field = ParseCharacterSet(value);
            _substitutions = field;
        }
    }

    /// <inheritdoc />
    public string Substitutions
    {
        get => _substitutions;

        init
        {
            _substitutions = ParseSubstitutions(CharacterSet, value);
            _substitutionsInitialised = true;
        }
    }

    /// <inheritdoc />
    public int SubstitutionCount
    {
        get
        {
            int count = 0;

            for (int i = 0; i < CharacterSet.Length; i++)
            {
                if (CharacterSet[i] != Substitutions[i])
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <inheritdoc />
    public char GetSubstitution(char letter)
    {
        int subsIndex = CharacterSet.IndexOf(letter, StringComparison.Ordinal);
        return subsIndex < 0 ? letter : Substitutions[subsIndex];
    }

    /// <inheritdoc />
    public void SetSubstitution(char substitution, char newSubstitution)
    {
        char from = substitution;
        int fromIndex = CharacterSet.IndexOf(from, StringComparison.Ordinal);

        if (fromIndex < 0)
        {
            throw new ArgumentException("Substitution must be a valid character.", nameof(substitution));
        }

        char to = newSubstitution;
        int toIndex = CharacterSet.IndexOf(to, StringComparison.Ordinal);

        if (toIndex < 0)
        {
            throw new ArgumentException("Substitution must be a valid character.", nameof(newSubstitution));
        }

        if (Substitutions[fromIndex] == to)
        {
            // Trying to set the same as already set
            return;
        }

        char fromSubs = Substitutions[fromIndex];
        int toInvIndex = Substitutions.IndexOf(to, StringComparison.Ordinal);

        char[] temp = [.. Substitutions];
        temp[fromIndex] = to;
        temp[toInvIndex] = fromSubs;
        _substitutions = new string(temp);
    }

    /// <inheritdoc />
    public char Reverse(char letter)
    {
        if (CharacterSet.IndexOf(letter, StringComparison.Ordinal) < 0)
        {
            return letter;
        }

        // Substitutions is a permutation of CharacterSet, so the letter that substitutes to this
        // one sits at the same position in CharacterSet as this one does in Substitutions.
        int index = Substitutions.IndexOf(letter, StringComparison.Ordinal);
        return index < 0 ? letter : CharacterSet[index];
    }

    /// <inheritdoc />
    public bool Equals(MonoAlphabeticSettings? other)
        => other is not null
            && string.Equals(CharacterSet, other.CharacterSet, StringComparison.Ordinal)
            && string.Equals(Substitutions, other.Substitutions, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(CharacterSet, Substitutions);

    private static string ParseCharacterSet(string characterSet)
    {
        if (string.IsNullOrWhiteSpace(characterSet))
        {
            throw new ArgumentException("Invalid number of characters.", nameof(characterSet));
        }

        foreach (char character in characterSet)
        {
            if (!char.IsLetter(character))
            {
                throw new ArgumentException("All characters must be letters.", nameof(characterSet));
            }
        }

        return characterSet.Length != characterSet.Distinct().Count()
            ? throw new ArgumentException("Characters must not be duplicated.", nameof(characterSet))
            : characterSet;
    }

    private static string ParseSubstitutions(string characterSet, string substitutions)
    {
        if (string.IsNullOrWhiteSpace(substitutions)
            || substitutions.Length != characterSet.Length)
        {
            throw new ArgumentException("Incorrect number of substitutions.", nameof(substitutions));
        }

        foreach (char character in substitutions)
        {
            if (!char.IsLetter(character))
            {
                throw new ArgumentException("All substitutions must be letters.", nameof(substitutions));
            }
        }

        return substitutions.Length != substitutions.Distinct().Count()
            ? throw new ArgumentException("Substitutions must not be duplicated.", nameof(substitutions))
            : !substitutions.All(characterSet.Contains)
            ? throw new ArgumentException("Substitutions must be in the character set.", nameof(substitutions))
            : substitutions;
    }
}
