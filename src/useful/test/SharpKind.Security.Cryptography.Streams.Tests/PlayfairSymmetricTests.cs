// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

public class PlayfairSymmetricTests
{
    [Fact]
    public void Ctor()
    {
        using SymmetricAlgorithm cipher = new PlayfairSymmetric();
        Assert.Equal([], cipher.Key);
        Assert.Equal([], cipher.IV);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PLAYFAIREXAMPLE")]
    public void KeyValid(string keyword)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyword);
        using SymmetricAlgorithm cipher = new PlayfairSymmetric { Key = key };
        Assert.Equal(key, cipher.Key);
    }

    [Theory]
    [InlineData("KE Y")]
    [InlineData("KEY1")]
    public void KeyInvalid(string keyword)
    {
        byte[] key = Encoding.Unicode.GetBytes(keyword);
        using SymmetricAlgorithm cipher = new PlayfairSymmetric();
        Assert.Throws<ArgumentException>("value", () => cipher.Key = key);
    }

    [Fact]
    public void EncryptDecrypt()
    {
        const string plaintext = "HIDETHEGOLDINTHETREXESTUMP";
        const string ciphertext = "BMODZBXDNABEKUDMUIXMMOUVIF";

        using SymmetricAlgorithm cipher = new PlayfairSymmetric { Key = Encoding.Unicode.GetBytes("PLAYFAIREXAMPLE") };

        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void TransformIsRepeatable()
    {
        using SymmetricAlgorithm cipher = new PlayfairSymmetric { Key = Encoding.Unicode.GetBytes("KEY") };
        string first = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "HELLO WORLD");
        Assert.Equal(first, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "HELLO WORLD"));
    }

    [Fact]
    public void Name()
    {
        using SymmetricAlgorithm cipher = new PlayfairSymmetric();
        Assert.Equal("Playfair", cipher.ToString());
    }
}
