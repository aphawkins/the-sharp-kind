// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class VigenereTests
{
    public static TheoryData<string, string, string> Data => new()
    {
        { "ATTACKATDAWN", "LXFOPVEFRNHR", "LEMON" },
    };

    [Fact]
    public void CtorSettings()
    {
        Vigenere cipher = new(new VigenereSettings() { Keyword = "Hello" });
        Assert.Equal("HELLO", cipher.Settings.Keyword);
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ATTACKATDAWN", "LxFoPvEfRnHr", "LEMON")]
    [InlineData("ATTACKATDAWN", "ATTACKatDAWN", "")]
    public void Decrypt(string plaintext, string ciphertext, string keyword)
    {
        Vigenere cipher = new(new VigenereSettings());
        cipher.Settings.Keyword = keyword;
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("AtTaCkAtDaWn", "LXFOPVEFRNHR", "LEMON")]
    [InlineData("ATTACKatDAWN", "ATTACKATDAWN", "")]
    public void Encrypt(string plaintext, string ciphertext, string keyword)
    {
        Vigenere cipher = new(new VigenereSettings());
        cipher.Settings.Keyword = keyword;
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Fact]
    public void GenerateSettings()
    {
        Vigenere cipher = new(new VigenereSettings());

        const int testsCount = 5;
        string[] keywords = new string[testsCount];
        for (int i = 0; i < testsCount; i++)
        {
            cipher.GenerateSettings();
            keywords[i] = cipher.Settings.Keyword;
        }

        Assert.True(keywords.Distinct().Count() > 1);
        Assert.All(keywords, keyword => Assert.Matches("^[A-Z]{1,25}$", keyword));
    }

    [Theory]
    [InlineData("LEM0N")] // Digit
    [InlineData("LEM ON")] // Space
    [InlineData("LEM-ON")] // Punctuation
    public void KeywordInvalidCharacters(string keyword)
        => Assert.Throws<ArgumentException>("Keyword", () => new VigenereSettings() { Keyword = keyword });

    [Fact]
    public void KeywordTooLong()
        => Assert.Throws<ArgumentOutOfRangeException>(
            "Keyword",
            () => new VigenereSettings() { Keyword = new string('A', 26) });

    [Theory]
    [InlineData("ATTACK AT DAWN!", "LXFOPV EF RNHR!", "LEMON")] // Non-letters pass through and don't consume the keyword
    public void EncryptSkipsNonLetters(string plaintext, string ciphertext, string keyword)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        Vigenere cipher = new(new VigenereSettings() { Keyword = keyword });
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
        Assert.Equal(plaintext.ToUpperInvariant(), cipher.Decrypt(ciphertext));
    }

    [Fact]
    public void Name()
    {
        Vigenere cipher = new(new VigenereSettings());
        Assert.Equal("Vigenere", cipher.CipherName);
        Assert.Equal("Vigenere", cipher.ToString());
    }
}
