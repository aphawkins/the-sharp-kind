// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Book cipher settings.
/// </summary>
public interface IBookCipherSettings
{
    /// <summary>
    /// Gets the key text. Only its letters A-Z are counted, ignoring case, and the first letter is number 1.
    /// </summary>
    public string KeyText { get; }
}
