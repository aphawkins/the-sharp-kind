// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Playfair cipher settings.
/// </summary>
public interface IPlayfairSettings
{
    /// <summary>
    /// Gets or sets the keyword that seeds the 5x5 grid.
    /// </summary>
    public string Keyword { get; set; }

    /// <summary>
    /// Gets the 25 letters of the grid, row by row. J is merged with I.
    /// </summary>
    public string Grid { get; }
}
