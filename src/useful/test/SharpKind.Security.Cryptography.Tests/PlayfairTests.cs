// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class PlayfairTests
{
    public static TheoryData<string, string, string> Data => new()
    {
        { "HIDETHEGOLDINTHETREXESTUMP", "BMODZBXDNABEKUDMUIXMMOUVIF", "PLAYFAIREXAMPLE" },
    };

    [Fact]
    public void CtorSettings()
    {
        Playfair cipher = new(new PlayfairSettings() { Keyword = "Hello" });
        Assert.Equal("HELLO", cipher.Settings.Keyword);
        Assert.Equal("HELOABCDFGIKMNPQRSTUVWXYZ", cipher.Settings.Grid);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Decrypt(string plaintext, string ciphertext, string keyword)
    {
        Playfair cipher = new(new PlayfairSettings() { Keyword = keyword });
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Encrypt(string plaintext, string ciphertext, string keyword)
    {
        Playfair cipher = new(new PlayfairSettings() { Keyword = keyword });
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Fact]
    public void EncryptSeparatesDoubledLetters()
    {
        Playfair cipher = new(new PlayfairSettings());
        Assert.Equal("BALXLOON", cipher.Decrypt(cipher.Encrypt("BALLOON")));
    }

    [Fact]
    public void EncryptPadsOddLetterCount()
    {
        Playfair cipher = new(new PlayfairSettings());
        Assert.Equal("ABCX", cipher.Decrypt(cipher.Encrypt("ABC")));
    }

    [Fact]
    public void EncryptMergesJWithI()
    {
        Playfair cipher = new(new PlayfairSettings() { Keyword = "KEY" });
        Assert.Equal(cipher.Encrypt("IN"), cipher.Encrypt("JN"));
    }

    [Fact]
    public void EncryptDropsNonLetters()
    {
        Playfair cipher = new(new PlayfairSettings());
        Assert.Equal(cipher.Encrypt("ATTACKATDAWN"), cipher.Encrypt("Attack at dawn! 12"));
    }

    [Fact]
    public void DecryptOddLetterCount()
    {
        Playfair cipher = new(new PlayfairSettings());
        Assert.Throws<ArgumentException>("ciphertext", () => cipher.Decrypt("ABC"));
    }

    [Theory]
    [InlineData("KE Y")]
    [InlineData("KEY1")]
    public void KeywordInvalidCharacters(string keyword)
        => Assert.Throws<ArgumentException>("Keyword", () => new PlayfairSettings() { Keyword = keyword });

    [Fact]
    public void Name()
    {
        Playfair cipher = new(new PlayfairSettings());
        Assert.Equal("Playfair", cipher.CipherName);
        Assert.Equal("Playfair", cipher.ToString());
    }

    [Fact]
    public void RejectsNull()
    {
        Playfair cipher = new(new PlayfairSettings());
        Assert.Throws<ArgumentNullException>(() => cipher.Encrypt(null!));
        Assert.Throws<ArgumentNullException>(() => cipher.Decrypt(null!));
    }
}
