// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class AdfgvxTests
{
    public static TheoryData<string, string, string, string, string> Data => new()
    {
        { "ABC", "XYXXXY", "XY", "ABCD", "BA" },
        { "ABC", "XYXXXY", "XY", "ABCD", "ba" },
    };

    [Fact]
    public void CtorDefault()
    {
        Adfgvx cipher = new(new AdfgvxSettings());
        Assert.Equal("ADFGVX", cipher.Settings.Labels);
        Assert.Equal(36, cipher.Settings.Square.Length);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Decrypt(string plaintext, string ciphertext, string labels, string square, string keyword)
    {
        Adfgvx cipher = new(new AdfgvxSettings(labels, square, keyword));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Encrypt(string plaintext, string ciphertext, string labels, string square, string keyword)
    {
        Adfgvx cipher = new(new AdfgvxSettings(labels, square, keyword));
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Theory]
    [InlineData("ATTACKAT1200AM", "PRIVACY")]
    [InlineData("ATTACKAT1200AM", "AA")] // Repeated keyword letters keep their column order
    [InlineData("A", "LONGERKEYWORD")] // Shorter text than the keyword
    public void RoundTrip(string plaintext, string keyword)
    {
        Adfgvx cipher = new(new AdfgvxSettings("ADFGVX", "PH0QG64MEA1YL2NOFDXKR3CVS5ZW7BJ9UTI8", keyword));
        Assert.Equal(plaintext, cipher.Decrypt(cipher.Encrypt(plaintext)));
    }

    [Fact]
    public void RoundTripEightLabels()
    {
        // The shape of Singh's stage 7: eight labels and a 64 character square.
        string square = string.Concat(Enumerable.Range('!', 64).Select(static c => (char)c));
        Adfgvx cipher = new(new AdfgvxSettings("CEMOPRTU", square, "SINGH"));
        Assert.Equal(64, cipher.Settings.Square.Length);
        Assert.Equal("ATTACKATDAWN", cipher.Decrypt(cipher.Encrypt("Attack at dawn")));
    }

    [Fact]
    public void SquareIsCaseSensitive()
    {
        Adfgvx cipher = new(new AdfgvxSettings("XY", "Aa B", "BA"));
        Assert.NotEqual(cipher.Encrypt("A"), cipher.Encrypt("a"));
        Assert.Equal("Aa B", cipher.Decrypt(cipher.Encrypt("Aa B")));
    }

    [Fact]
    public void EncryptFallsBackToUpperCase()
    {
        Adfgvx cipher = new(new AdfgvxSettings("XY", "ABCD", "BA"));
        Assert.Equal(cipher.Encrypt("ABC"), cipher.Encrypt("abc"));
    }

    [Fact]
    public void EncryptDropsCharactersNotInSquare()
    {
        Adfgvx cipher = new(new AdfgvxSettings());
        Assert.Equal(cipher.Encrypt("ATTACK"), cipher.Encrypt("Attack!"));
    }

    [Fact]
    public void DecryptOddLabelCount()
    {
        Adfgvx cipher = new(new AdfgvxSettings());
        Assert.Throws<ArgumentException>("ciphertext", () => cipher.Decrypt("ADF"));
    }

    [Theory]
    [InlineData("AAA", "ABCDEFGHI", "KEY")] // Repeated label
    [InlineData("AB", "ABCDE", "KEY")] // Square wrong size
    [InlineData("AB", "ABCC", "KEY")] // Repeated square character
    [InlineData("A B", "ABCDEFGHI", "KEY")] // Space in labels
    [InlineData("AB", "Aa Aa", "KEY")] // Repeated square character
    [InlineData("", "", "KEY")] // Nothing
    public void InvalidSettings(string labels, string square, string keyword)
        => Assert.Throws<ArgumentException>(() => new AdfgvxSettings(labels, square, keyword));

    [Fact]
    public void EmptyKeyword()
        => Assert.Throws<ArgumentException>("keyword", () => new AdfgvxSettings("AB", "ABCD", string.Empty));

    [Fact]
    public void Name()
    {
        Adfgvx cipher = new(new AdfgvxSettings());
        Assert.Equal("ADFGVX", cipher.CipherName);
        Assert.Equal("ADFGVX", cipher.ToString());
    }

    [Fact]
    public void RejectsNull()
    {
        Adfgvx cipher = new(new AdfgvxSettings());
        Assert.Throws<ArgumentNullException>(() => cipher.Encrypt(null!));
        Assert.Throws<ArgumentNullException>(() => cipher.Decrypt(null!));
    }
}
