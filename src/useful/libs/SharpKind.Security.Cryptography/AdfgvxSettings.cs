// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Settings for the ADFGVX style cipher. The defaults are the classic ADFGVX labels and a plain A-Z 0-9 square.
/// The square is case sensitive and may hold a space, so a 64 cell square can hold both cases of the letters.
/// </summary>
public sealed record AdfgvxSettings : IAdfgvxSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdfgvxSettings"/> class.
    /// </summary>
    public AdfgvxSettings()
        : this("ADFGVX", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", "KEY")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdfgvxSettings"/> class.
    /// </summary>
    /// <param name="labels">The distinct labels of the square's rows and columns, which are not white space.</param>
    /// <param name="square">The distinct characters of the square, row by row.</param>
    /// <param name="keyword">The transposition keyword.</param>
    /// <exception cref="ArgumentException">
    /// The labels must be distinct and not white space, the square must hold the labels' count squared
    /// distinct characters, and the keyword must not be empty.
    /// </exception>
    public AdfgvxSettings(string labels, string square, string keyword)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(square);
        ArgumentException.ThrowIfNullOrEmpty(keyword);

        Labels = Validate(labels.ToUpperInvariant(), nameof(labels), allowWhiteSpace: false);
        Square = Validate(square, nameof(square), allowWhiteSpace: true);
        Keyword = keyword.ToUpperInvariant();

        if (Square.Length != Labels.Length * Labels.Length)
        {
            throw new ArgumentException("The square must hold the labels' count squared characters.", nameof(square));
        }
    }

    /// <inheritdoc />
    public string Labels { get; }

    /// <inheritdoc />
    public string Square { get; }

    /// <inheritdoc />
    public string Keyword { get; }

    private static string Validate(string text, string paramName, bool allowWhiteSpace)
        => text.Length == 0
            || text.Distinct().Count() != text.Length
            || (!allowWhiteSpace && text.Any(char.IsWhiteSpace))
            ? throw new ArgumentException("Characters must be distinct, and labels cannot be white space.", paramName)
            : text;
}
