// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// ADFGVX style cipher settings: a Polybius square followed by a columnar transposition.
/// </summary>
public interface IAdfgvxSettings
{
    /// <summary>
    /// Gets the labels of the square's rows and columns.
    /// </summary>
    public string Labels { get; }

    /// <summary>
    /// Gets the square, row by row. It holds <c>Labels.Length</c> squared distinct characters, and is case sensitive.
    /// </summary>
    public string Square { get; }

    /// <summary>
    /// Gets the keyword that orders the columns of the transposition.
    /// </summary>
    public string Keyword { get; }
}
