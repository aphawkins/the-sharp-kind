// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class ReflectorTests
{
    public static TheoryData<string, string, string, string> Data => new()
    {
        { "ABC", "ABC", "ABC", "ABC" }, // Default settings
        { "ABC", "BAC", "ABC", "BAC" }, // Simple substitution
        { "ABCD", "BADC", "ABCD", "BADC" }, // All characters subtituted
        { "ABC", "ABC", "Å", "Å" }, // Invalid character
        { "ABC", "BAC", "AB C", "BA C" }, // Space
    };

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "BADCFEHGIJKLMNOPQRSTUVWXYZ", "HELLOWORLD", "GfLlOwOrLc")]
    public void Decrypt(string characterSet, string substitutions, string plaintext, string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(characterSet);
        ArgumentNullException.ThrowIfNull(substitutions);

        IReflectorSettings settings = new ReflectorSettings()
        {
            CharacterSet = characterSet.ToCharArray(),
            Substitutions = substitutions.ToCharArray(),
        };
        Reflector cipher = new(settings);
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "BADCFEHGIJKLMNOPQRSTUVWXYZ", "HeLlOwOrLd", "GFLLOWORLC")]
    public void EncryptCipher(string characterSet, string substitutions, string plaintext, string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(characterSet);
        ArgumentNullException.ThrowIfNull(substitutions);

        IReflectorSettings settings = new ReflectorSettings()
        {
            CharacterSet = characterSet.ToCharArray(),
            Substitutions = substitutions.ToCharArray(),
        };
        Reflector cipher = new(settings);
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Fact]
    public void GenerateSettings()
    {
        Reflector cipher = new(new ReflectorSettings());

        const int testsCount = 5;
        for (int i = 0; i < testsCount; i++)
        {
            cipher.GenerateSettings();
            Assert.NotEqual(cipher.Settings.CharacterSet, cipher.Settings.Substitutions);
            foreach (char c in cipher.Settings.CharacterSet)
            {
                Assert.NotEqual(cipher.Settings.GetSubstitution(c), c);

                // Each substitution is its own inverse.
                Assert.Equal(c, cipher.Settings.GetSubstitution(cipher.Settings.GetSubstitution(c)));
            }
        }
    }

    [Fact]
    public void Name()
    {
        IReflectorSettings settings = new ReflectorSettings();
        Reflector cipher = new(settings);
        Assert.Equal("Reflector", cipher.CipherName);
        Assert.Equal("Reflector", cipher.ToString());
    }
}
