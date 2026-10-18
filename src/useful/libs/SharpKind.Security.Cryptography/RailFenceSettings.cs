// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Settings for the Rail Fence cipher.
/// </summary>
public sealed record RailFenceSettings : IRailFenceSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RailFenceSettings"/> class.
    /// </summary>
    public RailFenceSettings() => Rails = 2;

    /// <summary>
    /// Gets the most rails allowed.
    /// </summary>
    public static int MaxRails => 25;

    /// <summary>
    /// Gets or sets the number of rails.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Value must be between 2 and 25.</exception>
    public int Rails
    {
        get;

        set
        {
            if (value < 2 || value > MaxRails)
            {
                throw new ArgumentOutOfRangeException(nameof(Rails), $"Value must be between 2 and {MaxRails}.");
            }

            field = value;
        }
    }
}
