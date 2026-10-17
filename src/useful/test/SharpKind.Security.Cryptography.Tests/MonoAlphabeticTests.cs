// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class MonoAlphabeticTests
{
    public static TheoryData<string, string, string, string> Data => new()
    {
        { "ABC", "BAC", "ABC", "BAC" },
        { "ABCD", "BADC", "ABCD", "BADC" },
        { "ABC", "BCA", "ABC", "BCA" },
        { "ABC", "ABC", "ABC", "ABC" },
        { "ABCD", "ABCD", "Å", "Å" },
        { "ABCD", "BADC", "AB CD", "BA DC" },
    };

    [Fact]
    public void Ctor()
    {
        const string characterSet = "ABC";
        const string substitutions = "ABC";

        IMonoAlphabeticSettings settings = new MonoAlphabeticSettings() { CharacterSet = characterSet, Substitutions = substitutions };
        MonoAlphabetic cipher = new(settings);
        Assert.Equal(characterSet, cipher.Settings.CharacterSet);
        Assert.Equal(substitutions, cipher.Settings.Substitutions);
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "BADCFEHGIJKLMNOPQRSTUVWXYZ", "GFLLOWORLC", "HeLlOwOrLd")]
    public void Decrypt(string characterSet, string substitutions, string plaintext, string ciphertext)
    {
        IMonoAlphabeticSettings settings = new MonoAlphabeticSettings() { CharacterSet = characterSet, Substitutions = substitutions };
        MonoAlphabetic cipher = new(settings);
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "BADCFEHGIJKLMNOPQRSTUVWXYZ", "HeLlOwOrLd", "GFLLOWORLC")]
    public void Encrypt(string characterSet, string substitutions, string plaintext, string ciphertext)
    {
        IMonoAlphabeticSettings settings = new MonoAlphabeticSettings() { CharacterSet = characterSet, Substitutions = substitutions };
        MonoAlphabetic cipher = new(settings);
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Fact]
    public void GenerateSettings()
    {
        MonoAlphabetic cipher = new(new MonoAlphabeticSettings());

        const int testsCount = 5;
        for (int i = 0; i < testsCount; i++)
        {
            cipher.GenerateSettings();
            Assert.NotEqual(cipher.Settings.CharacterSet, cipher.Settings.Substitutions);
            char previous = 'A';
            bool isSequential = true;
            foreach (char c in cipher.Settings.CharacterSet)
            {
                isSequential &= previous < cipher.Settings.GetSubstitution(c);
                previous = cipher.Settings.GetSubstitution(c);
            }

            Assert.False(isSequential);
        }
    }

    [Fact]
    public void Name()
    {
        IMonoAlphabeticSettings settings = new MonoAlphabeticSettings();
        MonoAlphabetic cipher = new(settings);
        Assert.Equal("MonoAlphabetic", cipher.CipherName);
        Assert.Equal("MonoAlphabetic", cipher.ToString());
    }
}
