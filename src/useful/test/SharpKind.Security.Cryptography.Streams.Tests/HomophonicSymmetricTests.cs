// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class HomophonicSymmetricTests
{
    /// <summary>
    /// Gets a key where every letter maps to itself followed by A, with E also mapping to EB.
    /// </summary>
    private static string KeyString
        => string.Join('|', Enumerable.Range('A', 26).Select(static c => $"{(char)c}:{(c == 'E' ? "EA,EB" : $"{(char)c}A")}"));

    [Fact]
    public void Ctor()
    {
        using SymmetricAlgorithm cipher = new HomophonicSymmetric();
        string key = string.Join('|', Enumerable.Range('A', 26).Select(static c => $"{(char)c}:{(char)c}"));
        Assert.Equal(Encoding.Unicode.GetBytes(key), cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Fact]
    public void KeyValid()
    {
        byte[] key = Encoding.Unicode.GetBytes(KeyString);
        using SymmetricAlgorithm cipher = new HomophonicSymmetric { Key = key };
        Assert.Equal(key, cipher.Key);
    }

    [Theory]
    [InlineData("")] // No key
    [InlineData("A|B")] // No homophones
    [InlineData("A:X|A:Y")] // Repeated plaintext character
    [InlineData("A:X|B")] // Entry without homophones
    public void KeyInvalid(string value)
    {
        byte[] key = Encoding.Unicode.GetBytes(value);
        using SymmetricAlgorithm cipher = new HomophonicSymmetric();
        Assert.Throws<ArgumentException>(nameof(value), () => cipher.Key = key);
    }

    [Fact]
    public void KeyInvalidDuplicateSymbol()
    {
        byte[] key = Encoding.Unicode.GetBytes(KeyString.Replace("B:BA", "B:AA", StringComparison.Ordinal));
        using SymmetricAlgorithm cipher = new HomophonicSymmetric();
        Assert.Throws<ArgumentException>("value", () => cipher.Key = key);
    }

    [Fact]
    public void EncryptDecrypt()
    {
        using SymmetricAlgorithm cipher = new HomophonicSymmetric { Key = Encoding.Unicode.GetBytes(KeyString) };

        Assert.Equal("EAEBEAEB", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "EEEE"));
        Assert.Equal("EEEE", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, "EAEBEAEB"));
    }

    [Fact]
    public void GenerateKey()
    {
        using SymmetricAlgorithm cipher = new HomophonicSymmetric { Key = Encoding.Unicode.GetBytes(KeyString) };

        cipher.GenerateKey();

        Assert.NotEqual(Encoding.Unicode.GetBytes(KeyString), cipher.Key);
        string ciphertext = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "HELLO, WORLD!");
        Assert.Equal("HELLO, WORLD!", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new HomophonicSymmetric();
        Assert.Equal("Homophonic", cipher.ToString());
    }
}
