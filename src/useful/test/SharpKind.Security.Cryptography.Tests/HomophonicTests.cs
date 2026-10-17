// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class HomophonicTests
{
    /// <summary>
    /// Gets settings where every letter maps to itself followed by A, with E also mapping to EB.
    /// </summary>
    private static HomophonicSettings Settings
        => new(Enumerable.Range('A', 26).ToDictionary(
            static c => (char)c,
            static c => (IReadOnlyList<string>)(c == 'E' ? ["EA", "EB"] : [$"{(char)c}A"])));

    [Fact]
    public void CtorDefault()
    {
        HomophonicSettings settings = new();
        Assert.Equal(1, settings.SymbolLength);
        Assert.Equal(26, settings.Homophones.Count);
    }

    [Fact]
    public void CtorSettings()
    {
        Homophonic cipher = new(Settings);
        Assert.Equal(2, cipher.Settings.SymbolLength);
    }

    [Theory]
    [InlineData("EEEE", "EAEBEAEB")]
    [InlineData("Hello, World!", "HAEALALAOA, WAOARALADA!")]
    public void Encrypt(string plaintext, string ciphertext)
    {
        Homophonic cipher = new(Settings);
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Theory]
    [InlineData("EEEE", "EAEBEAEB")]
    [InlineData("HELLO, WORLD!", "HAEALALAOA, WAOARALADA!")]
    public void Decrypt(string plaintext, string ciphertext)
    {
        Homophonic cipher = new(Settings);
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Fact]
    public void DecryptUnknownSymbol()
    {
        Homophonic cipher = new(Settings);
        Assert.Throws<ArgumentException>("ciphertext", () => cipher.Decrypt("ZZ"));
        Assert.Throws<ArgumentException>("ciphertext", () => cipher.Decrypt("A"));
    }

    [Fact]
    public void EncryptLetterWithoutHomophones()
    {
        Homophonic cipher = new(new HomophonicSettings(new Dictionary<char, IReadOnlyList<string>> { ['A'] = ["X"] }));
        Assert.Equal("XX, 1", cipher.Encrypt("aa, 1"));
        Assert.Throws<ArgumentException>("plaintext", () => cipher.Encrypt("B"));
    }

    [Fact]
    public void NonLetterPlaintext()
    {
        Homophonic cipher = new(new HomophonicSettings(new Dictionary<char, IReadOnlyList<string>>
        {
            ['A'] = ["M", "N"],
            [' '] = ["X"],
        }));

        Assert.Equal("MXNXM", cipher.Encrypt("a a a"));
        Assert.Equal("A A A", cipher.Decrypt("MXNXM"));
    }

    [Fact]
    public void NoPlaintextCharacters()
        => Assert.Throws<ArgumentException>("homophones", () => new HomophonicSettings(new Dictionary<char, IReadOnlyList<string>>()));

    [Theory]
    [InlineData("AA")] // Already used by another letter
    [InlineData("ba")] // Not upper case
    [InlineData("A A")] // White space
    [InlineData("ZZZ")] // Wrong length
    [InlineData("")] // Empty
    public void InvalidHomophone(string symbol)
    {
        Dictionary<char, IReadOnlyList<string>> homophones = Settings.Homophones.ToDictionary();
        homophones['B'] = [symbol];
        Assert.Throws<ArgumentException>("homophones", () => new HomophonicSettings(homophones));
    }

    [Fact]
    public void LowerCasePlaintextCharacter()
    {
        Dictionary<char, IReadOnlyList<string>> homophones = new() { ['a'] = ["X"] };
        Assert.Throws<ArgumentException>("homophones", () => new HomophonicSettings(homophones));
    }

    [Fact]
    public void NoHomophonesForCharacter()
    {
        Dictionary<char, IReadOnlyList<string>> homophones = new() { ['A'] = [] };
        Assert.Throws<ArgumentException>("homophones", () => new HomophonicSettings(homophones));
    }

    [Fact]
    public void Name()
    {
        Homophonic cipher = new(new HomophonicSettings());
        Assert.Equal("Homophonic", cipher.CipherName);
        Assert.Equal("Homophonic", cipher.ToString());
    }

    [Fact]
    public void RejectsNull()
    {
        Homophonic cipher = new(new HomophonicSettings());
        Assert.Throws<ArgumentNullException>(() => cipher.Encrypt(null!));
        Assert.Throws<ArgumentNullException>(() => cipher.Decrypt(null!));
    }
}
