// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Affine key generator.
/// </summary>
internal static class AffineSettingsGenerator
{
    public static AffineSettings Generate()
        => new()
        {
            Multiplier = AffineSettings.ValidMultipliers[
                RandomNumberGenerator.GetInt32(AffineSettings.ValidMultipliers.Count)],
            Shift = RandomNumberGenerator.GetInt32(Alphabet.Length),
        };
}
