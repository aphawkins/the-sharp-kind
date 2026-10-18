// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class AdfgvxSymmetricTests
{
    [Fact]
    public void Ctor()
    {
        using SymmetricAlgorithm cipher = new AdfgvxSymmetric();
        Assert.Equal(Encoding.Unicode.GetBytes("ADFGVX|ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789|KEY"), cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Fact]
    public void KeyValid()
    {
        byte[] key = Encoding.Unicode.GetBytes("XY|ABCD|BA");
        using SymmetricAlgorithm cipher = new AdfgvxSymmetric { Key = key };
        Assert.Equal(key, cipher.Key);
    }

    [Theory]
    [InlineData("")] // No key
    [InlineData("XY|ABCD")] // Too few parts
    [InlineData("XY|ABCD|BA|")] // Too many parts
    [InlineData("XX|ABCD|BA")] // Repeated label
    [InlineData("XY|ABC|BA")] // Square wrong size
    [InlineData("XY|ABCD|")] // No keyword
    public void KeyInvalid(string value)
    {
        byte[] key = Encoding.Unicode.GetBytes(value);
        using SymmetricAlgorithm cipher = new AdfgvxSymmetric();
        Assert.Throws<ArgumentException>(nameof(value), () => cipher.Key = key);
    }

    [Fact]
    public void EncryptDecrypt()
    {
        using SymmetricAlgorithm cipher = new AdfgvxSymmetric { Key = Encoding.Unicode.GetBytes("XY|ABCD|BA") };

        Assert.Equal("XYXXXY", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "ABC"));
        Assert.Equal("ABC", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, "XYXXXY"));
    }

    [Fact]
    public void GenerateKey()
    {
        using SymmetricAlgorithm cipher = new AdfgvxSymmetric();
        byte[] key = cipher.Key;

        cipher.GenerateKey();

        Assert.NotEqual(key, cipher.Key);
        string ciphertext = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "ATTACK AT 1200AM");
        Assert.Equal("ATTACKAT1200AM", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new AdfgvxSymmetric();
        Assert.Equal("ADFGVX", cipher.ToString());
    }
}
