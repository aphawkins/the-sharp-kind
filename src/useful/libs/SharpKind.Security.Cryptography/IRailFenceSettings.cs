// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Rail Fence cipher settings.
/// </summary>
public interface IRailFenceSettings
{
    /// <summary>
    /// Gets or sets the number of rails.
    /// </summary>
    public int Rails { get; set; }
}
