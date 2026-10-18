// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class ColumnarTests
{
    [Theory]
    [InlineData("WEAREDISCOVEREDFLEEATONCE", "EVLNACDTESEAROFODEECWIREE", "ZEBRAS")]
    [InlineData("WE ARE DISCOVERED!", "EV ACD ESERODEWIR!", "ZEBRAS")] // Non-letters stay where they are
    [InlineData("ABCD", "ACBD", "AA")] // Repeated keyword letters read left to right
    [InlineData("HELLO", "HELLO", "A")] // One column reads straight back
    public void EncryptDecrypt(string plaintext, string ciphertext, string keyword)
    {
        Columnar cipher = new(new ColumnarSettings() { Keyword = keyword });
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Fact]
    public void KeywordUpperCased()
        => Assert.Equal("ZEBRAS", new ColumnarSettings() { Keyword = "zebras" }.Keyword);

    [Theory]
    [InlineData("ZEB4AS")]
    [InlineData("ZEB AS")]
    public void KeywordInvalidCharacters(string keyword)
        => Assert.Throws<ArgumentException>("Keyword", () => new ColumnarSettings() { Keyword = keyword });

    [Theory]
    [InlineData("")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    public void KeywordLengthOutOfRange(string keyword)
        => Assert.Throws<ArgumentOutOfRangeException>("Keyword", () => new ColumnarSettings() { Keyword = keyword });

    [Fact]
    public void GenerateSettings()
    {
        Columnar cipher = new(new ColumnarSettings());

        const int testsCount = 5;
        string[] keywords = new string[testsCount];
        for (int i = 0; i < testsCount; i++)
        {
            cipher.GenerateSettings();
            keywords[i] = cipher.Settings.Keyword;
        }

        Assert.True(keywords.Distinct().Count() > 1);
        Assert.All(keywords, keyword => Assert.Matches("^[A-Z]{2,25}$", keyword));
    }

    [Fact]
    public void Name()
    {
        Columnar cipher = new(new ColumnarSettings());
        Assert.Equal("Columnar", cipher.CipherName);
        Assert.Equal("Columnar", cipher.ToString());
    }
}
