// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Rail Fence key generator.
/// </summary>
internal static class RailFenceSettingsGenerator
{
    public static RailFenceSettings Generate()
        => new() { Rails = RandomNumberGenerator.GetInt32(2, RailFenceSettings.MaxRails + 1) };
}
