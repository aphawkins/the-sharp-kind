// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class BeaufortSymmetricTests
{
    [Fact]
    public void Ctor()
    {
        using SymmetricAlgorithm cipher = new BeaufortSymmetric();
        Assert.Equal([], cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Theory]
    [InlineData("")]
    [InlineData("FORTIFICATION")]
    public void KeyValid(string keyString)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyString);
        using SymmetricAlgorithm cipher = new BeaufortSymmetric { Key = key };
        Assert.Equal(key, cipher.Key);
    }

    [Theory]
    [InlineData("KE Y")]
    [InlineData("KEY1")]
    public void KeyInvalid(string keyString)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyString);
        using SymmetricAlgorithm cipher = new BeaufortSymmetric();
        Assert.Throws<ArgumentException>("value", () => cipher.Key = key);
    }

    [Fact]
    public void EncryptDecrypt()
    {
        const string plaintext = "DEFEND THE EAST!";
        const string ciphertext = "CKMPVC PVW PIWU!";

        using SymmetricAlgorithm cipher = new BeaufortSymmetric { Key = Encoding.Unicode.GetBytes("FORTIFICATION") };

        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void GenerateKey()
    {
        using SymmetricAlgorithm cipher = new BeaufortSymmetric();

        cipher.GenerateKey();

        Assert.Matches("^[A-Z]{1,25}$", Encoding.Unicode.GetString(cipher.Key));
        string ciphertext = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "Hello, World!");
        Assert.Equal("HELLO, WORLD!", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new BeaufortSymmetric();
        Assert.Equal("Beaufort", cipher.ToString());
    }
}
