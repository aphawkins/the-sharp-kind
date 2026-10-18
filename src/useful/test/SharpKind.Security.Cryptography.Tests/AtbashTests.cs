// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class AtbashTests
{
    public static TheoryData<string, string> Data => new()
    {
        { "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "ZYXWVUTSRQPONMLKJIHGFEDCBA" },
        { ">?@ [\\]", ">?@ [\\]" },
        { "Å", "Å" },
    };

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "zyxwvutsrqponmlkjihgfedcba")]
    public void DecryptCipher(string plaintext, string ciphertext)
    {
        Atbash cipher = new();
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("abcdefghijklmnopqrstuvwxyz", "ZYXWVUTSRQPONMLKJIHGFEDCBA")]
    public void EncryptCipher(string plaintext, string ciphertext)
    {
        Atbash cipher = new();
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Fact]
    public void GenerateSettingsChangesNothing()
    {
        // Atbash has no key, so there is nothing to generate.
        Atbash cipher = new();
        cipher.GenerateSettings();
        Assert.Equal("ZYX", cipher.Encrypt("ABC"));
    }

    [Fact]
    public void Name()
    {
        Atbash cipher = new();
        Assert.Equal("Atbash", cipher.CipherName);
        Assert.Equal("Atbash", cipher.ToString());
    }
}
