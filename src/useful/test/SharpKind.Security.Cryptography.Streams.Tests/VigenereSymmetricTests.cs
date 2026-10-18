// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class VigenereSymmetricTests
{
    [Fact]
    public void Ctor()
    {
        using SymmetricAlgorithm cipher = new VigenereSymmetric();
        Assert.Equal([], cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Theory]
    [InlineData("")]
    [InlineData("LEMON")]
    public void KeyValid(string keyString)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyString);
        using SymmetricAlgorithm cipher = new VigenereSymmetric { Key = key };
        Assert.Equal(key, cipher.Key);
    }

    [Theory]
    [InlineData("KE Y")]
    [InlineData("KEY1")]
    public void KeyInvalid(string keyString)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyString);
        using SymmetricAlgorithm cipher = new VigenereSymmetric();
        Assert.Throws<ArgumentException>("value", () => cipher.Key = key);
    }

    [Fact]
    public void EncryptDecrypt()
    {
        const string plaintext = "ATTACK AT DAWN!";
        const string ciphertext = "LXFOPV EF RNHR!";

        using SymmetricAlgorithm cipher = new VigenereSymmetric { Key = Encoding.Unicode.GetBytes("LEMON") };

        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void GenerateKey()
    {
        using SymmetricAlgorithm cipher = new VigenereSymmetric();

        cipher.GenerateKey();

        Assert.Matches("^[A-Z]{1,25}$", Encoding.Unicode.GetString(cipher.Key));
        string ciphertext = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "Hello, World!");
        Assert.Equal("HELLO, WORLD!", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new VigenereSymmetric();
        Assert.Equal("Vigenere", cipher.ToString());
    }
}
