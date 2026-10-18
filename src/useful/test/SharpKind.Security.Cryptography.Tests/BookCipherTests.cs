// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class BookCipherTests
{
    private const string KeyText = "Hello, World!";  // Letters: HELLOWORLD

    [Theory]
    [InlineData("HELLO", "1 2 3 3 5")]
    [InlineData("world", "6 5 8 3 10")]
    [InlineData("He llo!", "1 2 3 3 5")]
    [InlineData("", "")]
    public void Encrypt(string plaintext, string ciphertext)
    {
        BookCipher cipher = new(new BookCipherSettings(KeyText));
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Theory]
    [InlineData("HELLO", "1 2 3 3 5")]
    [InlineData("HELLO", "1 2 4 3 5")] // Any occurrence of a letter decrypts
    [InlineData("HELLO", "1\n2  3\t3 5")] // Any white space separates positions
    [InlineData("", "")]
    public void Decrypt(string plaintext, string ciphertext)
    {
        BookCipher cipher = new(new BookCipherSettings(KeyText));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Fact]
    public void EncryptLetterNotInKeyText()
    {
        BookCipher cipher = new(new BookCipherSettings(KeyText));
        Assert.Throws<ArgumentException>("plaintext", () => cipher.Encrypt("QUIZ"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("11")] // Past the end of the ten letters
    [InlineData("-1")]
    [InlineData("A")]
    [InlineData("1.5")]
    public void DecryptInvalidPosition(string ciphertext)
    {
        BookCipher cipher = new(new BookCipherSettings(KeyText));
        Assert.Throws<ArgumentException>(nameof(ciphertext), () => cipher.Decrypt(ciphertext));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123 !?")]
    public void KeyTextWithoutLetters(string keyText)
        => Assert.Throws<ArgumentException>(nameof(keyText), () => new BookCipherSettings(keyText));

    [Fact]
    public void GenerateSettingsKeepsTheKeyText()
    {
        // Both sides must already have the key text, so a random one is of no use.
        BookCipherSettings settings = new(KeyText);
        BookCipher cipher = new(settings);

        cipher.GenerateSettings();

        Assert.Same(settings, cipher.Settings);
        Assert.Equal("1 2 3 3 5", cipher.Encrypt("HELLO"));
    }

    [Fact]
    public void Name()
    {
        BookCipher cipher = new(new BookCipherSettings(KeyText));
        Assert.Equal("Book", cipher.CipherName);
        Assert.Equal("Book", cipher.ToString());
    }

    [Fact]
    public void RejectsNull()
    {
        BookCipher cipher = new(new BookCipherSettings(KeyText));
        Assert.Throws<ArgumentNullException>(() => cipher.Encrypt(null!));
        Assert.Throws<ArgumentNullException>(() => cipher.Decrypt(null!));
    }
}
