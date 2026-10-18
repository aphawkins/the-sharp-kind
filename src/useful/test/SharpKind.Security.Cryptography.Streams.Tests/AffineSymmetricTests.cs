// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class AffineSymmetricTests
{
    [Fact]
    public void Ctor()
    {
        using SymmetricAlgorithm cipher = new AffineSymmetric();
        Assert.Equal(Encoding.Unicode.GetBytes("1,0"), cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Theory]
    [InlineData("1,0")]
    [InlineData("5,8")]
    [InlineData("25,25")]
    public void KeyValid(string keyString)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyString);
        using SymmetricAlgorithm cipher = new AffineSymmetric { Key = key };
        Assert.Equal(key, cipher.Key);
    }

    [Theory]
    [InlineData("2,0")]
    [InlineData("5,26")]
    [InlineData("5,-1")]
    public void KeyInvalid(string keyString)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyString);
        using SymmetricAlgorithm cipher = new AffineSymmetric();
        Assert.Throws<ArgumentOutOfRangeException>("key", () => cipher.Key = key);
    }

    [Fact]
    public void EncryptDecrypt()
    {
        const string plaintext = "AFFINE CIPHER!";
        const string ciphertext = "IHHWVC SWFRCP!";

        using SymmetricAlgorithm cipher = new AffineSymmetric { Key = Encoding.Unicode.GetBytes("5,8") };

        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void GenerateKey()
    {
        using SymmetricAlgorithm cipher = new AffineSymmetric();

        cipher.GenerateKey();

        Assert.Matches("^[0-9]+,[0-9]+$", Encoding.Unicode.GetString(cipher.Key));
        string ciphertext = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "Hello, World!");
        Assert.Equal("HELLO, WORLD!", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new AffineSymmetric();
        Assert.Equal("Affine", cipher.ToString());
    }
}
