// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

/// <summary>
/// Checks the Enigma against <see cref="EnigmaReferenceVectors"/>.
/// </summary>
public class EnigmaReferenceTests
{
    [Theory]
    [MemberData(nameof(EnigmaReferenceVectors.Vectors), MemberType = typeof(EnigmaReferenceVectors))]
    public void Encrypt(string reflector, string wheels, string rings, string plugs, string start, string plaintext, string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(plugs);

        Enigma enigma = new(EnigmaFactory.Settings(reflector, wheels, rings, plugs, start));
        Assert.Equal(ciphertext, enigma.Encrypt(plaintext));
    }

    [Theory]
    [MemberData(nameof(EnigmaReferenceVectors.Vectors), MemberType = typeof(EnigmaReferenceVectors))]
    public void Decrypt(string reflector, string wheels, string rings, string plugs, string start, string plaintext, string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(plugs);

        Enigma enigma = new(EnigmaFactory.Settings(reflector, wheels, rings, plugs, start));
        Assert.Equal(plaintext, enigma.Decrypt(ciphertext));
    }
}
