// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Settings for the Affine cipher.
/// </summary>
public sealed record AffineSettings : IAffineSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AffineSettings"/> class.
    /// </summary>
    public AffineSettings() => Multiplier = 1;

    /// <summary>
    /// Gets the multipliers that are coprime with 26, and so can be undone.
    /// </summary>
    public static IReadOnlyList<int> ValidMultipliers { get; } = [1, 3, 5, 7, 9, 11, 15, 17, 19, 21, 23, 25];

    /// <summary>
    /// Gets or sets the multiplier of the cipher.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Value must be coprime with 26 and between 1 and 25.</exception>
    public int Multiplier
    {
        get;

        set
        {
            if (!ValidMultipliers.Contains(value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Multiplier),
                    "Value must be one of 1, 3, 5, 7, 9, 11, 15, 17, 19, 21, 23 or 25.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the shift of the cipher.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Value must be between 0 and 25.</exception>
    public int Shift
    {
        get;

        set
        {
            if (value is < 0 or > 25)
            {
                throw new ArgumentOutOfRangeException(nameof(Shift), "Value must be between 0 and 25.");
            }

            field = value;
        }
    }
}
