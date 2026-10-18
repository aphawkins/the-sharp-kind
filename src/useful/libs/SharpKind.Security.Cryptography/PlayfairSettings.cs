// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Settings for the Playfair cipher.
/// </summary>
public sealed record PlayfairSettings : IPlayfairSettings
{
    /// <summary>
    /// The letters of the grid, which has no J.
    /// </summary>
    internal const string GridLetters = "ABCDEFGHIKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// Gets or sets the keyword that seeds the 5x5 grid.
    /// </summary>
    /// <exception cref="ArgumentException">The keyword must be letters of the alphabet.</exception>
    public string Keyword
    {
        get => field ?? string.Empty;

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            foreach (char letter in value)
            {
                if (Alphabet.IndexOf(letter) < 0)
                {
                    throw new ArgumentException("All keyword characters must be letters.", nameof(Keyword));
                }
            }

            field = value.ToUpperInvariant();
            Grid = BuildGrid(field);
        }
    }

    /// <inheritdoc />
    public string Grid { get; private set; } = GridLetters;

    private static string BuildGrid(string keyword)
    {
        StringBuilder grid = new(GridLetters.Length);

        foreach (char letter in keyword.Replace('J', 'I') + GridLetters)
        {
            if (grid.ToString().Contains(letter, StringComparison.Ordinal))
            {
                continue;
            }

            grid.Append(letter);
        }

        return grid.ToString();
    }
}
