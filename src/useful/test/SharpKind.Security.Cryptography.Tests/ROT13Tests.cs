// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class ROT13Tests
{
    public static TheoryData<string, string> Data => new()
    {
        { "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "NOPQRSTUVWXYZABCDEFGHIJKLM" },
        { ">?@ [\\]", ">?@ [\\]" },
        { "Å", "Å" },
    };

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "nopqrstuvwxyzabcdefghijklm")]
    public void DecryptCipher(string plaintext, string ciphertext)
    {
        Rot13 cipher = new();
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Theory]
    [MemberData(nameof(Data))]
    [InlineData("abcdefghijklmnopqrstuvwxyz", "NOPQRSTUVWXYZABCDEFGHIJKLM")]
    public void EncryptCipher(string plaintext, string ciphertext)
    {
        Rot13 cipher = new();
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Fact]
    public void GenerateSettingsChangesNothing()
    {
        // ROT13 always rotates by 13, so there is nothing to generate.
        Rot13 cipher = new();
        cipher.GenerateSettings();
        Assert.Equal("NOP", cipher.Encrypt("ABC"));
    }

    [Fact]
    public void Name()
    {
        Rot13 cipher = new();
        Assert.Equal("ROT13", cipher.CipherName);
        Assert.Equal("ROT13", cipher.ToString());
    }
}
