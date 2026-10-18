// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Columnar transposition cipher settings.
/// </summary>
public interface IColumnarSettings
{
    /// <summary>
    /// Gets or sets the keyword, whose letters order the columns.
    /// </summary>
    public string Keyword { get; set; }
}
