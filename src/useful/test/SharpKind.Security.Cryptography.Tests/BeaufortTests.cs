// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class BeaufortTests
{
    [Theory]
    [InlineData("DEFENDTHEEASTWALLOFTHECASTLE", "CKMPVCPVWPIWUJOGIUAPVWRIWUUK", "FORTIFICATION")]
    [InlineData("DEFEND THE EAST!", "CKMPVC PVW PIWU!", "FORTIFICATION")] // Non-letters don't consume the keyword
    [InlineData("ABC", "AZY", "")] // An empty keyword behaves like "A"
    public void EncryptDecrypt(string plaintext, string ciphertext, string keyword)
    {
        Beaufort cipher = new(new VigenereSettings() { Keyword = keyword });
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Fact]
    public void IsReciprocal()
    {
        Beaufort cipher = new(new VigenereSettings() { Keyword = "KEY" });
        Assert.Equal(cipher.Encrypt("SECRET"), cipher.Decrypt("SECRET"));
    }

    [Fact]
    public void GenerateSettings()
    {
        Beaufort cipher = new(new VigenereSettings());

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
        Beaufort cipher = new(new VigenereSettings());
        Assert.Equal("Beaufort", cipher.CipherName);
        Assert.Equal("Beaufort", cipher.ToString());
    }
}
