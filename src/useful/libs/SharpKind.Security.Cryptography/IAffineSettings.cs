// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Affine cipher settings.
/// </summary>
public interface IAffineSettings
{
    /// <summary>
    /// Gets or sets the multiplier, which must be coprime with 26.
    /// </summary>
    public int Multiplier { get; set; }

    /// <summary>
    /// Gets or sets the shift added after multiplying.
    /// </summary>
    public int Shift { get; set; }
}
