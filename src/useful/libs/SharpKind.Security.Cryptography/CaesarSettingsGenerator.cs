// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Caesar key generator.
/// </summary>
internal static class CaesarSettingsGenerator
{
    public static CaesarSettings Generate()
        => new() { RightShift = RandomNumberGenerator.GetInt32(Alphabet.Length) };
}
