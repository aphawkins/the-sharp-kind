// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class AutokeyTests
{
    [Theory]
    [InlineData("ATTACKATDAWN", "QNXEPKTMDCGN", "QUEEN")]
    [InlineData("ATTACK AT DAWN!", "QNXEPK TM DCGN!", "QUEEN")] // Non-letters don't extend the key
    [InlineData("ABC", "ABD", "")] // An empty keyword primes the key with "A"
    public void EncryptDecrypt(string plaintext, string ciphertext, string keyword)
    {
        Autokey cipher = new(new VigenereSettings() { Keyword = keyword });
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Fact]
    public void GenerateSettings()
    {
        Autokey cipher = new(new VigenereSettings());

        const int testsCount = 5;
        string[] keywords = new string[testsCount];
        for (int i = 0; i < testsCount; i++)
        {
            cipher.GenerateSettings();
            keywords[i] = cipher.Settings.Keyword;
        }

        Assert.True(keywords.Distinct().Count() > 1);
    }

    [Fact]
    public void Name()
    {
        Autokey cipher = new(new VigenereSettings());
        Assert.Equal("Autokey", cipher.CipherName);
        Assert.Equal("Autokey", cipher.ToString());
    }
}
